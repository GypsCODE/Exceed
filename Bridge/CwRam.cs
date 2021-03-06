﻿using ReadWriteProcessMemory;
using Resources;
using Resources.Utilities;

namespace Bridge {
    static class CwRam {
        public static Form1 form;
        public static ProcessMemory memory;
        public static int EntityStart {
            get { return memory.ReadInt(memory.ReadInt(memory.baseAddress + 0x0036b1c8) + 0x39C); }
        }

        public static int ItemStart {
            get { return EntityStart + 0x1E8 + form.listBoxItem.SelectedIndex * 280; }
        }
        public static int SlotStart {
            get { return ItemStart + 20 + 8 * form.listBoxSlots.SelectedIndex; }
        }

        public static int SkillStart {
            get { return EntityStart + 0x1138; }
        }

        public static void SetMode(Mode mode, int timer) {
            CwRam.memory.WriteInt(CwRam.EntityStart + 0x6C, timer);//skill timer
            CwRam.memory.WriteInt(CwRam.EntityStart + 0x68, (int)mode);//skill
        }

        public static void Teleport(LongVector destination) {
            CwRam.memory.WriteLong(CwRam.EntityStart + 0x10, destination.x);
            CwRam.memory.WriteLong(CwRam.EntityStart + 0x18, destination.y); 
            CwRam.memory.WriteLong(CwRam.EntityStart + 0x20, destination.z);
        }

        public static void Freeze(int duration) {
            CwRam.memory.WriteInt(CwRam.EntityStart + 0x134, duration);//ice spirit effect
        }

        public static void Fear(int duration) {
            CwRam.memory.WriteInt(CwRam.EntityStart + 0x130, duration);//ice spirit effect
        }
    }
}
