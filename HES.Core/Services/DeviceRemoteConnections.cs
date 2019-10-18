﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Hideez.SDK.Communication;
using Hideez.SDK.Communication.Remote;
using Hideez.SDK.Communication.Utils;

namespace HES.Core.Services
{
    public class DeviceRemoteConnections
    {
        class RemoteDeviceDescription
        {
            public IRemoteAppConnection AppConnection { get; }
            public TaskCompletionSource<RemoteDevice> Tcs { get; set; }
            public RemoteDevice Device { get; set; }

            public RemoteDeviceDescription(IRemoteAppConnection appConnection)
            {
                AppConnection = appConnection;
            }
        }

        const int channelNo = 4;

        readonly string _deviceId;

        readonly ConcurrentDictionary<string, RemoteDeviceDescription> _appConnections 
            = new ConcurrentDictionary<string, RemoteDeviceDescription>();

        public bool IsDeviceConnectedToHost => _appConnections.Count > 0;

        public DeviceRemoteConnections(string deviceId)
        {
            Debug.WriteLine($"+++++++++++++++ RemoteDeviceDescription {deviceId}");
            _deviceId = deviceId;
        }

        // device connected to the workstation, adding it to the list of the connected devices
        // overwrite if already exists
        public void OnDeviceConnected(string workstationId, IRemoteAppConnection appConnection)
        {
            Debug.WriteLine($"!!!!!!!!!!!!! OnDeviceConnected {_deviceId}");
            _appConnections.AddOrUpdate(workstationId, new RemoteDeviceDescription(appConnection), (conn, old) => 
            {
                return new RemoteDeviceDescription(appConnection);
            });
        }

        // device disconnected from the workstation, removing it from the list of the connected devices
        public void OnDeviceDisconnected(string workstationId)
        {
            Debug.WriteLine($"!!!!!!!!!!!!! OnDeviceDisconnected {_deviceId}");

            _appConnections.TryRemove(workstationId, out RemoteDeviceDescription _);
        }

        // workstation disconnected from the server, if this device has connections to this workstation, close them
        public void OnAppHubDisconnected(string workstationId)
        {
            Debug.WriteLine($"!!!!!!!!!!!!! OnAppHubDisconnected {_deviceId}");

            _appConnections.TryRemove(workstationId, out RemoteDeviceDescription _);
        }

        public async Task<RemoteDevice> ConnectDevice(string workstationId)
        {
            Debug.WriteLine($"!!!!!!!!!!!!! ConnectDevice {workstationId}");

            RemoteDeviceDescription descr = null;
            if (workstationId == null)
            {
                // trying to connect to any workstation, first, look for that where Device is not empty
                descr = _appConnections.Values.Where(x => x.Device != null).FirstOrDefault();
                if (descr == null)
                   descr = _appConnections.Values.FirstOrDefault();
            }
            else
            {
                _appConnections.TryGetValue(workstationId, out descr);
            }

            if (descr == null)
                throw new HideezException(HideezErrorCode.DeviceNotConnectedToAnyHost);

            TaskCompletionSource<RemoteDevice> tcs = null;
            lock (descr)
            {
                if (descr.Device != null)
                    return descr.Device;

                tcs = descr.Tcs;
                if (tcs == null)
                    descr.Tcs = new TaskCompletionSource<RemoteDevice>();
            }

            if (tcs != null)
                return await tcs.Task;

            try
            {
                // call Hideez Client to make remote channel
                await descr.AppConnection.EstablishRemoteDeviceConnection(_deviceId, channelNo);

                await descr.Tcs.Task.TimeoutAfter(20_000);

                return descr.Device;
            }
            catch (TimeoutException)
            {
                var ex = new HideezException(HideezErrorCode.RemoteConnectionTimedOut);
                descr.Tcs.TrySetException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                descr.Tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                lock(descr)
                {
                    descr.Tcs = null;
                }
            }
        }

        internal void OnDeviceHubConnected(string workstationId, IRemoteCommands caller)
        {
            Debug.WriteLine($"!!!!!!!!!!!!! OnDeviceHubConnected {workstationId}");

            Task.Run(async () => 
            {
                RemoteDeviceDescription descr = null;
                try
                {
                    _appConnections.TryGetValue(workstationId, out descr);
                    if (descr != null)
                    {
                        var remoteDevice = new RemoteDevice(_deviceId, caller, null, null); //new SdkLogger<RemoteDeviceConnectionsService>(_logger)
                        descr.Device = remoteDevice;

                        await remoteDevice.Verify(channelNo);

                        // Inform clients about connection ready
                        descr.Tcs.TrySetResult(remoteDevice);
                    }
                    else
                    {
                        Debug.WriteLine($"!!!!!!!!!!!!! ERROR Workstation Not Connected To Any Host {workstationId}");
                    }
                }
                catch (Exception ex)
                {
                    descr.Device = null;

                    // inform clients about connection fail
                    descr.Tcs?.TrySetException(ex);
                }
            });
        }

        internal void OnDeviceHubDisconnected(string workstationId)
        {
            _appConnections.TryRemove(workstationId, out RemoteDeviceDescription _);
        }

        internal RemoteDevice GetRemoteDevice(string workstationId)
        {
            _appConnections.TryGetValue(workstationId, out RemoteDeviceDescription descr);
            return descr?.Device;
        }
    }
}
