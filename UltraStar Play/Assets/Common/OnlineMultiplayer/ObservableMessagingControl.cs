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
        private const long MessageTimeoutInMillis = 5000;

        private readonly MessagingControl messagingControl;
        private readonly Func<ulong> ownLobbyMemberUnityNetcodeClientIdGetter;
        private readonly Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter;

        private readonly List<RunningRequestData> runningRequestDatas = new();
        private readonly object runningRequestDatasLock = new();

        public ObservableMessagingControl(
            MessagingControl messagingControl,
            Func<ulong> ownLobbyMemberUnityNetcodeClientIdGetter,
            Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter)
        {
            this.messagingControl = messagingControl;
            this.ownLobbyMemberUnityNetcodeClientIdGetter = ownLobbyMemberUnityNetcodeClientIdGetter;
            this.otherLobbyMemberUnityNetcodeClientIdsGetter = otherLobbyMemberUnityNetcodeClientIdsGetter;
        }

        public IObservable<NamedMessage> SendNamedMessageToAllClientsAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced)
        {
            List<ulong> targetNetcodeClientIds = new List<ulong>() { ownLobbyMemberUnityNetcodeClientIdGetter.Invoke() };
            targetNetcodeClientIds.AddRange(otherLobbyMemberUnityNetcodeClientIdsGetter.Invoke());

            return SendNamedMessageToClientsAsObservable(
                messageName,
                fastBufferWriter,
                targetNetcodeClientIds,
                reliableNetworkDelivery);
        }

        public IObservable<NamedMessage> SendNamedMessageToOtherClientsAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced)
        {
            IReadOnlyList<ulong> targetNetcodeClientIds = otherLobbyMemberUnityNetcodeClientIdsGetter.Invoke();
            return SendNamedMessageToClientsAsObservable(
                messageName,
                fastBufferWriter,
                targetNetcodeClientIds,
                reliableNetworkDelivery);
        }

        public IObservable<NamedMessage> SendNamedMessageToClientAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            ulong targetNetcodeClientId,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced)
        {
            return SendNamedMessageToClientsAsObservable(
                messageName,
                fastBufferWriter,
                new List<ulong>() { targetNetcodeClientId },
                reliableNetworkDelivery);
        }

        public IObservable<NamedMessage> SendNamedMessageToClientsAsObservable(
            string messageName,
            FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds,
            EReliableNetworkDelivery reliableNetworkDelivery = EReliableNetworkDelivery.ReliableSequenced)
        {
            string requestId = Guid.NewGuid().ToString();

            RunningRequestData runningRequestData = new RunningRequestData(
                requestId,
                messageName,
                targetNetcodeClientIds,
                reliableNetworkDelivery);

            lock (runningRequestDatasLock)
            {
                runningRequestDatas.Add(runningRequestData);
            }

            return Observable.Create<NamedMessage>(o =>
            {
                Debug.Log($"Sending observable request to {targetNetcodeClientIds.ToCsv(", ", "", "")}:  messageName: {messageName}, requestId: {requestId}");

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
                    o.OnError(new TimeoutException($"Received no response for message {messageName} with requestId {requestId} within {MessageTimeoutInMillis} ms"));
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
                    if (TimeUtils.IsDurationAboveThresholdInMillis(runningRequestData.MessageSendTimeInMillis, MessageTimeoutInMillis))
                    {
                        Debug.Log($"Timeout of observable message response with messageName: {runningRequestData.MessageName}, requestId: {runningRequestData.RequestId}");
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
                default: throw new IllegalArgumentException($"Cannot convert {reliableNetworkDelivery} to Netcode NetworkDelivery");
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

            public Action OnTimeout { get; set; }

            public RunningRequestData(
                string requestId,
                string messageName,
                IReadOnlyList<ulong> targetNetcodeClientIds,
                EReliableNetworkDelivery reliableNetworkDelivery)
            {
                RequestId = requestId;
                MessageName = messageName;
                TargetNetcodeClientIds = new List<ulong>(targetNetcodeClientIds);
                ReliableNetworkDelivery = reliableNetworkDelivery;

                receivedResponses = new();
                netcodeClientIdsWithoutResponse = new List<ulong>(targetNetcodeClientIds);

                MessageSendTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                OnTimeout = null;
            }

            public void AddResponse(NamedMessage request)
            {
                receivedResponses.Add(request);
                netcodeClientIdsWithoutResponse.Remove(request.SenderNetcodeClientId);
            }
        }

        public void RegisterAnswerableMessageHandler(string messageName, Action<AnswerableMessage> action)
        {
            messagingControl.RegisterNamedMessageHandler(
                messageName,
                request =>
                {
                    // Read responseMessageName,
                    // such that the response can be send directly to the corresponding recipient code
                    FastBufferReader fastBufferReader = request.MessagePayload;
                    fastBufferReader.ReadValueSafe(out string responseMessageName);

                    int originalMessageLength = fastBufferReader.Length - fastBufferReader.Position;
                    byte[] originalMessageBytes = new byte[originalMessageLength];
                    fastBufferReader.ReadBytesSafe(ref originalMessageBytes, originalMessageBytes.Length, 0);
                    using FastBufferReader originalMessageReader = new FastBufferReader(originalMessageBytes, Allocator.Temp);

                    AnswerableMessage answerableMessage = new(
                        request.SenderNetcodeClientId,
                        responseMessageName,
                        originalMessageReader);

                    action?.Invoke(answerableMessage);
                });
        }
    }
}
