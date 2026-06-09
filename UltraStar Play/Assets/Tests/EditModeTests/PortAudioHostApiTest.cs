using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PortAudioForUnity;
using UnityEngine;

namespace Editor.Tests
{
    public class PortAudioHostApiTest
    {
        [Test]
        public void ShouldIncludeAsioOnWindows()
        {
            if (!PlatformUtils.IsWindows)
            {
                Debug.Log("Not on Windows, skipping test");
                return;
            }

            List<PortAudioHostApi> hostApis = PortAudioUtils.HostApis
                .Select(portAudioHostApi => PortAudioConversionUtils.ConvertHostApi(portAudioHostApi))
                .ToList();
            Debug.Log("Host APIs: " + hostApis.JoinWith(", "));
            Assert.Contains(PortAudioHostApi.ASIO, hostApis);
        }
    }
}
