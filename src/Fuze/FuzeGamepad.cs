using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;
using ScpDriverInterface;
using System.Threading;
using System.Runtime.InteropServices;

namespace Fuze
{
    public class FuzeGamepad
    {
        private byte[] Vibration = { 0x02, 0x00, 0x00 };
        private Mutex rumble_mutex = new Mutex();

        public FuzeGamepad(HidDevice Device, ScpBus scpBus, int index)
        {
            Device.WriteFeatureData(Vibration);

            Thread rThread = new Thread(() => rumble_thread(Device));
            // rThread.Priority = ThreadPriority.BelowNormal; 
            rThread.Start();

            Thread iThread = new Thread(() => input_thread(Device, scpBus, index));
            iThread.Priority = ThreadPriority.Highest;
            iThread.Start();
        }

        //TODO: 震动
        private void rumble_thread(HidDevice Device)
        {
            byte[] local_vibration = { 0x02, 0x00, 0x00 };
            while (true)
            {
                rumble_mutex.WaitOne();
                if (local_vibration[2] != Vibration[2] || Vibration[1] != local_vibration[1])
                {
                    local_vibration[2] = Vibration[2];
                    local_vibration[1] = Vibration[1];
                    rumble_mutex.ReleaseMutex();
                    Device.WriteFeatureData(local_vibration);
                    //Console.WriteLine("Big Motor: {0}, Small Motor: {1}", Vibration[2], Vibration[1]);
                }
                else
                {
                    rumble_mutex.ReleaseMutex();
                }
                Thread.Sleep(20);
            }
        }

        private void input_thread(HidDevice Device, ScpBus scpBus, int index)
        {
            scpBus.PlugIn(index);
            X360Controller controller = new X360Controller();
            int timeout = 30;
            long last_changed = 0;
            //long last_mi_button = 0;
            while (true)
            {
                HidDeviceData data = Device.Read(timeout);
                var currentState = data.Data;
                bool changed = false;

                string str = Program.ByteArrayToHexString(currentState);
                //if (!string.IsNullOrEmpty(str))
                //    Console.WriteLine(Program.ByteArrayToHexString(currentState));


                if (data.Status == HidDeviceData.ReadStatus.Success && currentState.Length >= 10 && currentState[0] == 0x02)
                {
                    Console.WriteLine(Program.ByteArrayToHexString(currentState));
                    X360Buttons Buttons = X360Buttons.None;
                    if ((currentState[1] & 0x01) != 0) Buttons |= X360Buttons.A;
                    if ((currentState[1] & 0x02) != 0) Buttons |= X360Buttons.B;
                    if ((currentState[1] & 0x08) != 0) Buttons |= X360Buttons.X;
                    if ((currentState[1] & 0x10) != 0) Buttons |= X360Buttons.Y;
                    if ((currentState[1] & 0x40) != 0) Buttons |= X360Buttons.LeftBumper;
                    if ((currentState[1] & 0x80) != 0) Buttons |= X360Buttons.RightBumper;

                    if ((currentState[2] & 0x20) != 0) Buttons |= X360Buttons.LeftStick;
                    if ((currentState[2] & 0x40) != 0) Buttons |= X360Buttons.RightStick;

                    if (currentState[3] != 0x0F)
                    {
                        if (currentState[3] == 0 || currentState[3] == 1 || currentState[3] == 7) Buttons |= X360Buttons.Up;
                        if (currentState[3] == 4 || currentState[3] == 3 || currentState[3] == 5) Buttons |= X360Buttons.Down;
                        if (currentState[3] == 6 || currentState[3] == 5 || currentState[3] == 7) Buttons |= X360Buttons.Left;
                        if (currentState[3] == 2 || currentState[3] == 1 || currentState[3] == 3) Buttons |= X360Buttons.Right;
                    }

                    if ((currentState[2] & 0x04) != 0) Buttons |= X360Buttons.Start;
                    if ((currentState[2] & 0x08) != 0) Buttons |= X360Buttons.Back;

                    if ((currentState[2] & 0x10) != 0) Buttons |= X360Buttons.Logo;
                    //按下Fuze Logo键一下是不触发任何按键的，0x10是短按并松开fuze键时触发的按键
                    if ((currentState[2] & 0x01) != 0) Buttons |= X360Buttons.Logo;
                    //长按会触发0x01按键，在GNU/Linux系统下，会触发关机键

                    /*
                    //if ((currentState[20] & 1) != 0)
                    //{
                    //    last_mi_button = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
                    //    Buttons |= X360Buttons.Logo;
                    //}
                    //if (last_mi_button != 0) Buttons |= X360Buttons.Logo;
                    */

                    if (controller.Buttons != Buttons)
                    {
                        changed = true;
                        controller.Buttons = Buttons;
                    }

                    short LeftStickX = (short)((Math.Max(-127.0, currentState[4] - 128) / 127) * 32767);
                    if (LeftStickX == -32767)
                        LeftStickX = -32768;

                    if (LeftStickX != controller.LeftStickX)
                    {
                        changed = true;
                        controller.LeftStickX = LeftStickX;
                    }

                    short LeftStickY = (short)((Math.Max(-127.0, currentState[5] - 128) / 127) * -32767);
                    if (LeftStickY == -32767)
                        LeftStickY = -32768;

                    if (LeftStickY != controller.LeftStickY)
                    {
                        changed = true;
                        controller.LeftStickY = LeftStickY;
                    }

                    short RightStickX = (short)((Math.Max(-127.0, currentState[6] - 128) / 127) * 32767);
                    if (RightStickX == -32767)
                        RightStickX = -32768;

                    if (RightStickX != controller.RightStickX)
                    {
                        changed = true;
                        controller.RightStickX = RightStickX;
                    }

                    short RightStickY = (short)((Math.Max(-127.0, currentState[7] - 128) / 127) * -32767);
                    if (RightStickY == -32767)
                        RightStickY = -32768;

                    if (RightStickY != controller.RightStickY)
                    {
                        changed = true;
                        controller.RightStickY = RightStickY;
                    }

                    if (controller.LeftTrigger != currentState[8])
                    {
                        changed = true;
                        controller.LeftTrigger = currentState[8];
                    }

                    if (controller.RightTrigger != currentState[9])
                    {
                        changed = true;
                        controller.RightTrigger = currentState[9];

                    }
                }

                if (data.Status == HidDeviceData.ReadStatus.WaitTimedOut || (!changed && ((last_changed + timeout) < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond))))
                {
                    changed = true;
                }

                if (changed)
                {
                    //Console.WriteLine("changed");
                    //Console.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));
                    byte[] outputReport = new byte[8];
                    scpBus.Report(index, controller.GetReport(), outputReport);

                    //TODO: 震动
                    //if (outputReport[1] == 0x08)
                    //{
                    //    byte bigMotor = outputReport[3];
                    //    byte smallMotor = outputReport[4];
                    //    rumble_mutex.WaitOne();
                    //    if (bigMotor != Vibration[2] || Vibration[1] != smallMotor)
                    //    {
                    //        Vibration[1] = smallMotor;
                    //        Vibration[2] = bigMotor;
                    //    }
                    //    rumble_mutex.ReleaseMutex();
                    //}

                    /*
                    //if (last_mi_button != 0)
                    //{
                    //    if ((last_mi_button + 100) < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond))
                    //    {
                    //        last_mi_button = 0;
                    //        controller.Buttons ^= X360Buttons.Logo;
                    //    }
                    //}

                    //last_changed = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    */
                }

            }
        }
    }
}
