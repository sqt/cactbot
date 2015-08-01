﻿/*
 * Copyright (c) 2015 Tomonori Tamagawa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Advanced_Combat_Tracker;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cactbot
{
    public class FFXIVMemory : IDisposable
    {
        static class NativeMethods
        {
            // ReadProcessMemory
            [DllImport("kernel32.dll")]
            public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead);
        }
        
        private Thread _thread;
        private List<CombatantEP> _Combatants = new List<CombatantEP>();
        internal List<CombatantEP> Combatants
        {
            get
            {
                return this._Combatants;
            }
        }
        private object _CombatantsLock = new object();
        internal object CombatantsLock
        {
            get
            {
                return this._CombatantsLock;
            }
        }

        private const string charmapSignature32 = "81FEFFFF0000743581FE58010000732D8B3CB5";
        private const string charmapSignature64 = "48C1E8033DFFFF0000742B3DA80100007324488D0D";
        private const string targetSignature32  = "750E85D2750AB9";
        private const string targetSignature64  = "29017520483935";
        private const string enmitySignature32  = "E801A83300B9";
        private const string enmitySignature64  = "488D0D????????E820EF3F00488D0D"; //"488D0DB5C13C01E820EF3F00488D0D";
        private const int charmapOffset32 = 0;
        private const int charmapOffset64 = 0;
        private const int targetOffset32  = 88;
        private const int targetOffset64  = 0;
        private const int enmityOffset32  = 0;
        private const int enmityOffset64  = 0;

        private Process _process;
        private FFXIVClientMode _mode;

        private IntPtr charmapAddress = IntPtr.Zero;
        private IntPtr targetAddress = IntPtr.Zero;
        private IntPtr enmityAddress = IntPtr.Zero;
        private IntPtr aggroAddress = IntPtr.Zero;

        public FFXIVMemory(Process process)
        {
            _process = process;
            if (process.ProcessName == "ffxiv")
            {
                _mode = FFXIVClientMode.FFXIV_32;
            }
            else if (process.ProcessName == "ffxiv_dx11")
            {
                _mode = FFXIVClientMode.FFXIV_64;
            }
            else
            {
                _mode = FFXIVClientMode.Unknown;
            }

            this.getPointerAddress();

            _thread = new Thread(new ThreadStart(doScanCombatants));
            _thread.IsBackground = true;
            _thread.Start();

        }

        public void Dispose()
        {
            _thread.Abort();
        }

        private void doScanCombatants()
        {
            List<CombatantEP> c;
            while (true)
            {
                Thread.Sleep(200);

                if (!this.validateProcess())
                {
                    Thread.Sleep(1000);
                    return;
                }

                c = this._getCombatantList();
                lock (CombatantsLock)
                {
                    this._Combatants = c;
                }
            }
        }

        public enum FFXIVClientMode
        {
            Unknown = 0,
            FFXIV_32 = 1,
            FFXIV_64 = 2,
        }

        public Process process
        {
            get
            {
                return _process;
            }
        }

        public bool validateProcess()
        {
            if (_process == null)
            {
                return false;
            }
            if (_process.HasExited)
            {
                return false;
            }
            if (charmapAddress == IntPtr.Zero ||
                enmityAddress == IntPtr.Zero ||
                targetAddress == IntPtr.Zero)
            {
                return getPointerAddress();
            }
            return true;
        }

        /// <summary>
        /// 各ポインタのアドレスを取得
        /// </summary>
        private bool getPointerAddress()
        {
            bool success = true;
            string charmapSignature = charmapSignature32;
            string targetSignature = targetSignature32;
            string enmitySignature = enmitySignature32;
            int targetOffset = targetOffset32;
            int charmapOffset = charmapOffset32;
            int enmityOffset = enmityOffset32;

            bool bRIP = false;

            if (_mode == FFXIVClientMode.FFXIV_64)
            {
                bRIP = true;
                targetOffset = targetOffset64;
                charmapOffset = charmapOffset64;
                targetSignature = targetSignature64;
                charmapSignature = charmapSignature64;
                enmitySignature = enmitySignature64;
                enmityOffset = enmityOffset64;
            }

            /// CHARMAP
            List<IntPtr> list = SigScan(charmapSignature, 0, bRIP);
            if (list == null || list.Count == 0)
            {
                charmapAddress = IntPtr.Zero;
            }
            if (list.Count == 1)
            {
                charmapAddress = list[0] + charmapOffset;
            }
            if (charmapAddress == IntPtr.Zero)
            {
                success = false;
            }

            // ENMITY
            list = SigScan(enmitySignature, 0, bRIP);
            if (list == null || list.Count == 0)
            {
                enmityAddress = IntPtr.Zero;
            }
            if (list.Count == 1)
            {
                enmityAddress = list[0] + enmityOffset;
                aggroAddress = IntPtr.Add(enmityAddress, 0x900 + 8);
            }
            if (enmityAddress == IntPtr.Zero)
            {
                success = false;
            }

            /// TARGET
            list = SigScan(targetSignature, 0, bRIP);
            if (list == null || list.Count == 0)
            {
                targetAddress = IntPtr.Zero;
            }
            if (list.Count == 1)
            {
                targetAddress = list[0] + targetOffset;
            }
            if (targetAddress == IntPtr.Zero)
            {
                success = false;
            }


            if (success || charmapAddress != IntPtr.Zero)
            {
                CombatantEP c = GetSelfCombatant();
                if (c != null)
                {
                    //_overlay.LogDebug("MyCharacter: '{0}' ({1})", c.Name, c.ID);
                }
            }
            return success;
        }

        /// <summary>
        /// カレントターゲットの情報を取得
        /// </summary>
        public CombatantEP GetTargetCombatant()
        {
            CombatantEP target = null;
            IntPtr address = IntPtr.Zero;

            byte[] source = GetByteArray(targetAddress, 128);
            unsafe
            {
                if (_mode == FFXIVClientMode.FFXIV_64)
                {
                    fixed (byte* p = source) address = new IntPtr(*(Int64*)p);
                }
                else
                {
                    fixed (byte* p = source) address = new IntPtr(*(Int32*)p);
                }
            }
            if (address.ToInt64() <= 0)
            {
                return null;
            }

            source = GetByteArray(address, 0x3F40);
            target = GetCombatantFromByteArray(source);
            return target;
        }

        /// <summary>
        /// 自キャラの情報を取得
        /// </summary>
        public CombatantEP GetSelfCombatant()
        {
            CombatantEP self = null;
            IntPtr address = (IntPtr)GetUInt32(charmapAddress);
            if (address.ToInt64() > 0) {
                byte[] source = GetByteArray(address, 0x3F40);
                self = GetCombatantFromByteArray(source);
            }
            return self;
        }

        /// <summary>
        /// サークルターゲット情報を取得
        /// </summary>
        public CombatantEP GetAnchorCombatant()
        {
            CombatantEP self = null;
            int offset = _mode == FFXIVClientMode.FFXIV_64 ? 0x08 : 0x04;
            IntPtr address = (IntPtr)GetUInt32(targetAddress + offset);
            if (address.ToInt64() > 0)
            {
                byte[] source = GetByteArray(address, 0x3F40);
                self = GetCombatantFromByteArray(source);
            }
            return self;
        }

        /// <summary>
        /// フォーカスターゲット情報を取得
        /// </summary>
        public CombatantEP GetFocusCombatant()
        {
            CombatantEP self = null;
            int offset = _mode == FFXIVClientMode.FFXIV_64 ? 0x70 : 0x48;
            IntPtr address = (IntPtr)GetUInt32(targetAddress + offset);
            if (address.ToInt64() > 0)
            {
                byte[] source = GetByteArray(address, 0x3F40);
                self = GetCombatantFromByteArray(source);
            }
            return self;
        }

        /// <summary>
        /// ホバーターゲット情報を取得
        /// </summary>
        public CombatantEP GetHoverCombatant()
        {
            CombatantEP self = null;
            int offset = _mode == FFXIVClientMode.FFXIV_64 ? 0x30 : 0x18;
            IntPtr address = (IntPtr)GetUInt32(targetAddress + offset);
            if (address.ToInt64() > 0)
            {
                byte[] source = GetByteArray(address, 0x3F40);
                self = GetCombatantFromByteArray(source);
            }
            return self;
        }

        /// <summary>
        /// 周辺のキャラ情報を取得
        /// </summary>
        private unsafe List<CombatantEP> _getCombatantList()
        {
            int num = 344;
            List<CombatantEP> result = new List<CombatantEP>();

            int sz = (_mode == FFXIVClientMode.FFXIV_64) ? 8 : 4;
            byte[] source = GetByteArray(charmapAddress, sz * num);
            if (source == null || source.Length == 0) { return result; }

                for (int i = 0; i < num; i++)
                {
                    IntPtr p;
                    if (_mode == FFXIVClientMode.FFXIV_64)
                    {
                        fixed (byte* bp = source) p = new IntPtr(*(Int64*)&bp[i * sz]);
                    }
                    else
                    {
                        fixed (byte* bp = source) p = new IntPtr(*(Int32*)&bp[i * sz]);
                    }

                    if (!(p == IntPtr.Zero))
                    {
                        byte[] c = GetByteArray(p, 0x3F40);
                        CombatantEP combatant = GetCombatantFromByteArray(c);
                        if (combatant.type != ObjectType.PC && combatant.type != ObjectType.Monster)
                        {
                            continue;
                        }
                        if (combatant.ID != 0 && combatant.ID != 3758096384u && !result.Exists((CombatantEP x) => x.ID == combatant.ID))
                        {
                            combatant.Order = i;
                            result.Add(combatant);
                        }
                    }
                }

            return result;
        }

        /// <summary>
        /// メモリのバイト配列からキャラ情報に変換
        /// </summary>
        public unsafe CombatantEP GetCombatantFromByteArray(byte[] source)
        {
            int offset = 0;
            CombatantEP combatant = new CombatantEP();
            fixed (byte* p = source)
            {
                combatant.Name    = GetStringFromBytes(source, 48);
                combatant.ID      = *(uint*)&p[0x74];
                combatant.OwnerID = *(uint*)&p[0x84];
                if (combatant.OwnerID == 3758096384u)
                {
                    combatant.OwnerID = 0u;
                }
                combatant.type = (ObjectType)p[0x8A];
                combatant.EffectiveDistance = p[0x91];

                offset = (_mode == FFXIVClientMode.FFXIV_64) ? 176 : 160;
                combatant.PosX = *(Single*)&p[offset];
                combatant.PosZ = *(Single*)&p[offset + 4];
                combatant.PosY = *(Single*)&p[offset + 8];

                offset = (_mode == FFXIVClientMode.FFXIV_64) ? 448 : 392;
                combatant.TargetID = *(uint*)&p[offset];
                if (combatant.TargetID == 3758096384u)
                {
                    offset = (_mode == FFXIVClientMode.FFXIV_64) ? 2448 : 2520;
                    combatant.TargetID = *(uint*)&p[offset];
                }

                if (combatant.type == ObjectType.PC || combatant.type == ObjectType.Monster)
                {
                    offset = (_mode == FFXIVClientMode.FFXIV_64) ? 5872 : 5312;
                    combatant.Job       = p[offset];
                    combatant.Level     = p[offset + 1];
                    combatant.CurrentHP = *(int*)&p[offset + 8];
                    combatant.MaxHP     = *(int*)&p[offset + 12];
                    combatant.CurrentMP = *(int*)&p[offset + 16];
                    combatant.MaxMP     = *(int*)&p[offset + 20];
                    combatant.CurrentTP = *(short*)&p[offset + 24];
                    combatant.MaxTP     = 1000;
                }
                else
                {
                    combatant.CurrentHP =
                    combatant.MaxHP     =
                    combatant.CurrentMP =
                    combatant.MaxMP     =
                    combatant.MaxTP     =
                    combatant.CurrentTP = 0;
                }
            }
            return combatant;
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得
        /// </summary>
        public unsafe List<EnmityEntry> GetEnmityEntryList()
        {
            short num = 0;
            uint topEnmity = 0;
            List<EnmityEntry> result = new List<EnmityEntry>();
            List<CombatantEP> combatantList = Combatants;
            CombatantEP mychar = GetSelfCombatant();

            /// 一度に全部読む
            byte[] buffer = GetByteArray(enmityAddress, 0x900 + 2);
            fixed (byte* p = buffer) num = (short)p[0x900];

            if (num <= 0)
            {
                return result;
            }
            if (num > 32) num = 32;

            for (short i = 0; i < num; i++)
            {
                int p = i * 72;
                uint _id;
                uint _enmity;

                fixed (byte* bp = buffer)
                {
                    _id = *(uint*)&bp[p + 0x40];
                    _enmity = *(uint*)&bp[p + 0x44];
                }
                var entry = new EnmityEntry()
                {
                    ID = _id,
                    Enmity = _enmity,
                    isMe = false,
                    Name = "Unknown",
                    Job = 0x00
                };
                if (entry.ID > 0)
                {
                    CombatantEP c = combatantList.Find(x => x.ID == entry.ID);
                    if (c != null)
                    {
                        entry.Name = c.Name;
                        entry.Job = c.Job;
                        entry.OwnerID = c.OwnerID;
                    }
                    if (entry.ID == mychar.ID)
                    {
                        entry.isMe = true;
                    }
                    if (topEnmity <= entry.Enmity)
                    {
                        topEnmity = entry.Enmity;
                    }
                    entry.HateRate = (int)(((double)entry.Enmity / (double)topEnmity) * 100);
                    result.Add(entry);
                }
            }
            return result;
        }

        /// <summary>
        /// 敵視リスト情報を取得
        /// </summary>
        public unsafe List<AggroEntry> GetAggroList()
        {
            int num = 0;
            uint currentTargetID = 0;
            List<AggroEntry> result = new List<AggroEntry>();
            List<CombatantEP> combatantList = Combatants;
            CombatantEP mychar = GetSelfCombatant();

            // 一度に全部読む
            byte[] buffer = GetByteArray(aggroAddress, 32 * 72 + 2);

            fixed (byte* p = buffer) num = (short)p[0x900];
            if (num <= 0)
            {
                return result;
            }
            if (num > 32) num = 32;

            // current target
            currentTargetID = GetUInt32(aggroAddress, -4);
            if (currentTargetID == 3758096384u) currentTargetID = 0;
            //
            for (int i = 0; i < num; i++)
            {
                int p = i * 72;
                uint _id;
                short _enmity;

                fixed (byte* bp = buffer)
                {
                        _id = *(uint*)&bp[p + 64];
                        _enmity = (short)bp[p + 68];
                }

                var entry = new AggroEntry()
                {
                    ID = _id,
                    HateRate = _enmity,
                    Name = "Unknown",
                };
                if (entry.ID <= 0) continue;
                CombatantEP c = combatantList.Find(x => x.ID == entry.ID);
                if (c != null)
                {
                    entry.ID = c.ID;
                    entry.Order = c.Order;
                    entry.isCurrentTarget = (c.ID == currentTargetID);
                    entry.Name = c.Name;
                    entry.MaxHP = c.MaxHP;
                    entry.CurrentHP = c.CurrentHP;
                    if (c.TargetID > 0)
                    {
                        CombatantEP t = combatantList.Find(x => x.ID == c.TargetID);
                        if (t != null)
                        {
                            entry.Target = new EnmityEntry()
                            {
                                ID = t.ID,
                                Name = t.Name,
                                Job = t.Job,
                                OwnerID = t.OwnerID,
                                isMe = mychar.ID == t.ID ? true : false,
                                Enmity = 0,
                                HateRate = 0
                            };
                        }
                    }

                }
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// バイト配列からUTF-8文字列に変換
        /// </summary>
        private static string GetStringFromBytes(byte[] source, int offset = 0, int size = 256)
        {
            var bytes = new byte[size];
            Array.Copy(source, offset, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                {
                    continue;
                }
                realSize = i;
                break;
            }
            Array.Resize(ref bytes, realSize);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// バッファの長さだけメモリを読み取ってバッファに格納
        /// </summary>
        private bool Peek(IntPtr address, byte[] buffer)
        {
            IntPtr zero = IntPtr.Zero;
            IntPtr nSize = new IntPtr(buffer.Length);
            return NativeMethods.ReadProcessMemory(_process.Handle, address, buffer, nSize, ref zero);
        }

        /// <summary>
        /// メモリから指定された長さだけ読み取りバイト配列として返す
        /// </summary>
        /// <param name="address">読み取る開始アドレス</param>
        /// <param name="length">読み取る長さ</param>
        /// <returns></returns>
        private byte[] GetByteArray(IntPtr address, int length)
        {
            var data = new byte[length];
            Peek(address, data);
            return data;
        }

        /// <summary>
        /// メモリから4バイト読み取り32ビットIntegerとして返す
        /// </summary>
        /// <param name="address">読み取る位置</param>
        /// <param name="offset">オフセット</param>
        /// <returns></returns>
        private unsafe int GetInt32(IntPtr address, int offset = 0)
        {
            int ret;
            var value = new byte[4];
            Peek(IntPtr.Add(address,  offset), value);
            fixed (byte* p = &value[0]) ret = *(int*)p;
            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private unsafe uint GetUInt32(IntPtr address, int offset = 0)
        {
            uint ret;
            var value = new byte[4];
            Peek(IntPtr.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(uint*)p;
            return ret;
        }

        /// <summary>
        /// Signature scan.
        /// Read data at address which follow matched with the pattern and return it as a pointer.
        /// </summary>
        /// <param name="pattern">byte pattern signature</param>
        /// <param name="offset">offset to read</param>
        /// <param name="bRIP">x64 rip relative addressing mode if true</param>
        /// <returns>the pointer addresses</returns>
        private List<IntPtr> SigScan(string pattern, int offset = 0, bool bRIP = false)
        {
            IntPtr arg_05_0 = IntPtr.Zero;
            if (pattern == null || pattern.Length % 2 != 0)
            {
                return new List<IntPtr>();
            }

            byte?[] array = new byte?[pattern.Length / 2];
            for (int i = 0; i < pattern.Length / 2; i++)
            {
                string text = pattern.Substring(i * 2, 2);
                if (text == "??")
                {
                    array[i] = null;
                }
                else
                {
                    array[i] = new byte?(Convert.ToByte(text, 16));
                }
            }

            int num = 4096;

            int moduleMemorySize = _process.MainModule.ModuleMemorySize;
            IntPtr baseAddress = _process.MainModule.BaseAddress;
            IntPtr intPtr = IntPtr.Add(baseAddress, moduleMemorySize);
            IntPtr intPtr2 = baseAddress;
            byte[] array2 = new byte[num];
            List<IntPtr> list = new List<IntPtr>();
            while (intPtr2.ToInt64() < intPtr.ToInt64())
            {
                IntPtr zero = IntPtr.Zero;
                IntPtr nSize = new IntPtr(num);
                if (IntPtr.Add(intPtr2, num).ToInt64() > intPtr.ToInt64())
                {
                    nSize = (IntPtr)(intPtr.ToInt64() - intPtr2.ToInt64());
                }
                if (NativeMethods.ReadProcessMemory(_process.Handle, intPtr2, array2, nSize, ref zero))
                {
                    int num2 = 0;
                    while ((long)num2 < zero.ToInt64() - (long)array.Length - (long)offset - 4L + 1L)
                    {
                        int num3 = 0;
                        for (int j = 0; j < array.Length; j++)
                        {
                            if (!array[j].HasValue)
                            {
                                num3++;
                            }
                            else
                            {
                                if (array[j].Value != array2[num2 + j])
                                {
                                    break;
                                }
                                num3++;
                            }
                        }
                        if (num3 == array.Length)
                        {
                            IntPtr item;
                            if (bRIP)
                            {
                                item = new IntPtr(BitConverter.ToInt32(array2, num2 + array.Length + offset));
                                item = new IntPtr(intPtr2.ToInt64() + (long)num2 + (long)array.Length + 4L + item.ToInt64());
                            }
                            else if (_mode == FFXIVClientMode.FFXIV_64)
                            {
                                item = new IntPtr(BitConverter.ToInt64(array2, num2 + array.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }
                            else
                            {
                                item = new IntPtr(BitConverter.ToInt32(array2, num2 + array.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }
                            list.Add(item);
                        }
                        num2++;
                    }
                }
                intPtr2 = IntPtr.Add(intPtr2, num);
            }
            return list;
        }
    }
}
