using System;
using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class ObservableMessagingControl
    {
        private const long DefaultMessageTimeoutInMillis = 5000;

        private readonly IMessagingControl messagingControl;

        private readonly List<RunningRequestData> runningRequestDatas = new();
        private readonly object runningRequestDatasLock = new();

        public ObservableMessagingControl(
            IMessagingControl messagingControl)
        {
            this.messagingControl = messagingControl;
        }

        public IObservable<NamedMessage> SendNamedMessageToClientAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            ulong targetNetcodeClientId,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced,
            long timeoutInMillis = DefaultMessageTimeoutInMillis)
        {
            return SendNamedMessageToClientsAsObservable(
                messageName,
                fastBufferWriter,
                new ulong[] { targetNetcodeClientId },
                reliableNetworkDelivery,
                timeoutInMillis);
        }

        public IObservable<NamedMessage> SendNamedMessageToClientsAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced,
            long timeoutInMillis = DefaultMessageTimeoutInMillis)
        {
            if (messageName.IsNullOrEmpty())
            {
                return Observable.Throw<NamedMessage>(new ArgumentException($"{nameof(messageName)} cannot be empty"));
            }
            if (targetNetcodeClientIds.IsNullOrEmpty())
            {
                return Observable.Throw<NamedMessage>(new ArgumentException($"{nameof(targetNetcodeClientIds)} cannot be empty"));
            }

            string requestId = Guid.NewGuid().ToString();

            RunningRequestData runningRequestData = new RunningRequestData(
                requestId,
                messageName,
                targetNetcodeClientIds,
                reliableNetworkDelivery,
                timeoutInMillis);

            lock (runningRequestDatasLock)
            {
                runningRequestDatas.Add(runningRequestData);
            }

            return Observable.Create<NamedMessage>(o =>
            {
                Log.Debug(() => $"Sending observable request to targetNetcodeClientIds {targetNetcodeClientIds.JoinWith(", ")}:  messageName: {messageName}, requestId: {requestId}");

                IDisposable namedMessageHandlerDisposable = null;
                runningRequestData.OnTimeout = () =>
                {
                    try
                    {
                        namedMessageHandlerDisposable?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    // Notify subscribers
                    o.OnError(new TimeoutException($"Received no response for message {messageName} with requestId {requestId} within {timeoutInMillis} ms (targetNetcodeClientIds: {targetNetcodeClientIds.JoinWith(", ")}, networkDelivery: {reliableNetworkDelivery})"));
                };

                // Register handler for response message
                string responseMessageName = GetResponseMessageName(messageName, requestId);
                namedMessageHandlerDisposable = messagingControl.RegisterNamedMessageHandler(
                    responseMessageName,
                    response =>
                    {
                        Debug.Log($"Received response from Netcode client {response.SenderNetcodeClientId}, messageName: {messageName}, requestId: {requestId}");
                        runningRequestData.AddResponse(response);
                        try
                        {
                            o.OnNext(response);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            Debug.LogError($"Subscriber failed to handle response of message {messageName}");
                        }

                        if (runningRequestData.HasReceivedResponseFromEveryClient)
                        {
                            lock (runningRequestDatasLock)
                            {
                                runningRequestDatas.Remove(runningRequestData);
                            }

                            try
                            {
                                o.OnCompleted();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                                Debug.LogError($"Subscriber failed to handle completion of message {messageName}");
                            }

                            namedMessageHandlerDisposable?.Dispose();
                        }
                    });

                // Add responseMessageName to message payload,
                // such that the response can be sent directly to this recipient code
                int size = FastBufferWriter.GetWriteSize(responseMessageName)
                           + fastBufferWriter.Length;
                FastBufferWriter returnAddressableFastBufferWriter = new(size, Allocator.Temp);
                returnAddressableFastBufferWriter.WriteValueSafe(responseMessageName);
                returnAddressableFastBufferWriter.TryBeginWrite(fastBufferWriter.Length);
                returnAddressableFastBufferWriter.CopyFrom(fastBufferWriter);

                messagingControl.SendNamedMessageToClients(
                    messageName,
                    returnAddressableFastBufferWriter,
                    targetNetcodeClientIds,
                    ToNetcodeNetworkDelivery(reliableNetworkDelivery));

                return Disposable.Empty;
            });
        }

        private string GetResponseMessageName(string messageName, string requestId)
        {
            return $"Re:{messageName}:{requestId}";
        }

        public void UpdateMessageTimeout()
        {
            lock (runningRequestDatasLock)
            {
                for (int i = runningRequestDatas.Count - 1; i >= 0; i--)
                {
                    RunningRequestData runningRequestData = runningRequestDatas[i];
                    if (TimeUtils.IsDurationAboveThresholdInMillis(runningRequestData.MessageSendTimeInMillis, runningRequestData.TimeoutInMillis))
                    {
                        runningRequestData.OnTimeout?.Invoke();
                        runningRequestDatas.Remove(runningRequestData);
                    }
                }
            }
        }

        private NetworkDelivery ToNetcodeNetworkDelivery(EReliableNetworkDelivery reliableNetworkDelivery)
        {
            switch (reliableNetworkDelivery)
            {
                case EReliableNetworkDelivery.Reliable: return NetworkDelivery.Reliable;
                case EReliableNetworkDelivery.ReliableSequenced: return NetworkDelivery.ReliableSequenced;
                case EReliableNetworkDelivery.ReliableFragmentedSequenced: return NetworkDelivery.ReliableFragmentedSequenced;
                default: throw new ArgumentException($"Cannot convert {reliableNetworkDelivery} to Netcode NetworkDelivery");
            }
        }

        private class RunningRequestData
        {
            public string RequestId { get; private set; }
            public string MessageName { get; private set; }
            public IReadOnlyList<ulong> TargetNetcodeClientIds { get; private set; }
            public EReliableNetworkDelivery ReliableNetworkDelivery { get; private set; }

            public long MessageSendTimeInMillis { get; private set; }
            private readonly List<NamedMessage> receivedResponses;
            public IReadOnlyList<NamedMessage> ReceivedResponses => receivedResponses;

            private readonly List<ulong> netcodeClientIdsWithoutResponse;
            public bool HasReceivedResponseFromEveryClient => netcodeClientIdsWithoutResponse.IsNullOrEmpty();

            public long TimeoutInMillis { get; private set; }
            public Action OnTimeout { get; set; }

            public RunningRequestData(
                string requestId,
                string messageName,
                IReadOnlyList<ulong> targetNetcodeClientIds,
                EReliableNetworkDelivery reliableNetworkDelivery,
                long timeoutInMillis)
            {
                RequestId = requestId;
                MessageName = messageName;
                TargetNetcodeClientIds = new List<ulong>(targetNetcodeClientIds);
                ReliableNetworkDelivery = reliableNetworkDelivery;

                receivedResponses = new();
                netcodeClientIdsWithoutResponse = new List<ulong>(targetNetcodeClientIds);

                MessageSendTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                TimeoutInMillis = timeoutInMillis;
                OnTimeout = null;
            }

            public void AddResponse(NamedMessage request)
            {
                receivedResponses.Add(request);
                netcodeClientIdsWithoutResponse.Remove(request.SenderNetcodeClientId);
            }
        }

        public void SendResponseMessage(
            ObservedMessage observedMessage,
            FastBufferWriter messagePayload,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced)
        {
            messagingControl.SendNamedMessageToClient(
                observedMessage.ObservedMessageName,
                messagePayload,
                observedMessage.SenderNetcodeClientId,
                ToNetcodeNetworkDelivery(reliableNetworkDelivery));
        }

        public IDisposable RegisterObservedMessageHandler(string messageName, Action<ObservedMessage> action)
        {
            return messagingControl.RegisterNamedMessageHandler(
                messageName,
                request =>
                {
                    // Read responseMessageName,
                    // such that the response can be send directly to the corresponding recipient
                    FastBufferReader fastBufferReader = request.MessagePayload;
                    fastBufferReader.ReadValueSafe(out string responseMessageName);

                    int originalMessageLength = fastBufferReader.Length - fastBufferReader.Position;
                    byte[] originalMessageBytes = new byte[originalMessageLength];
                    fastBufferReader.ReadBytesSafe(ref originalMessageBytes, originalMessageBytes.Length, 0);
                    using FastBufferReader originalMessageReader = new FastBufferReader(originalMessageBytes, Allocator.Temp);

                    ObservedMessage observedMessage = new(
                        request.SenderNetcodeClientId,
                        responseMessageName,
                        originalMessageReader);

                    action?.Invoke(observedMessage);
                });
        }
    }
}
