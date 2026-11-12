// NPP plugin platform for .Net v0.94.00 by Kasper B. Graversen etc.
using System;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kbg.NppPluginNET.PluginInfrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NppData
    {
        public IntPtr _nppHandle;
        public IntPtr _scintillaMainHandle;
        public IntPtr _scintillaSecondHandle;
    }

    public delegate void NppFuncItemDelegate();

    [StructLayout(LayoutKind.Sequential)]
    public struct ShortcutKey
    {
        public ShortcutKey(bool isCtrl, bool isAlt, bool isShift, Keys key)
        {
            // the types 'bool' and 'char' have a size of 1 byte only!
            _isCtrl = Convert.ToByte(isCtrl);
            _isAlt = Convert.ToByte(isAlt);
            _isShift = Convert.ToByte(isShift);
            _key = Convert.ToByte(key);
        }
        public byte _isCtrl;
        public byte _isAlt;
        public byte _isShift;
        public byte _key;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FuncItem
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _itemName;
        public NppFuncItemDelegate _pFunc;
        public int _cmdID;
        public bool _init2Check;
        public ShortcutKey _pShKey;
    }

    public class FuncItems : IDisposable
    {
        List<FuncItem> _funcItems;
        int _sizeFuncItem;
        List<IntPtr> _shortCutKeys;
        IntPtr _nativePointer;
        bool _disposed = false;

        public FuncItems()
        {
            _funcItems = new List<FuncItem>();
            _sizeFuncItem = Marshal.SizeOf(typeof(FuncItem));
            _shortCutKeys = new List<IntPtr>();
        }

        [DllImport("kernel32")]
        static extern void RtlMoveMemory(IntPtr Destination, IntPtr Source, int Length);
        public void Add(FuncItem funcItem)
        {
            int oldSize = _funcItems.Count * _sizeFuncItem;
            _funcItems.Add(funcItem);
            int newSize = _funcItems.Count * _sizeFuncItem;
            IntPtr newPointer = Marshal.AllocHGlobal(newSize);

            if (_nativePointer != IntPtr.Zero)
            {
                RtlMoveMemory(newPointer, _nativePointer, oldSize);
                Marshal.FreeHGlobal(_nativePointer);
            }
            IntPtr ptrPosNewItem = (IntPtr)(newPointer.ToInt64() + oldSize);
            byte[] aB = Encoding.Unicode.GetBytes(funcItem._itemName + "\0");
            Marshal.Copy(aB, 0, ptrPosNewItem, aB.Length);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 128);
            IntPtr p = (funcItem._pFunc != null) ? Marshal.GetFunctionPointerForDelegate(funcItem._pFunc) : IntPtr.Zero;
            Marshal.WriteIntPtr(ptrPosNewItem, p);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + IntPtr.Size);
            Marshal.WriteInt32(ptrPosNewItem, funcItem._cmdID);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
            Marshal.WriteInt32(ptrPosNewItem, Convert.ToInt32(funcItem._init2Check));
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
            if (funcItem._pShKey._key != 0)
            {
                IntPtr newShortCutKey = Marshal.AllocHGlobal(4);
                Marshal.StructureToPtr(funcItem._pShKey, newShortCutKey, false);
                Marshal.WriteIntPtr(ptrPosNewItem, newShortCutKey);
            }
            else Marshal.WriteIntPtr(ptrPosNewItem, IntPtr.Zero);

            _nativePointer = newPointer;
        }

        public void RefreshItems()
        {
            IntPtr ptrPosItem = _nativePointer;
            for (int i = 0; i < _funcItems.Count; i++)
            {
                FuncItem updatedItem = new FuncItem();
                updatedItem._itemName = _funcItems[i]._itemName;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 128);
                updatedItem._pFunc = _funcItems[i]._pFunc;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);
                updatedItem._cmdID = Marshal.ReadInt32(ptrPosItem);
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
                updatedItem._init2Check = _funcItems[i]._init2Check;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
                updatedItem._pShKey = _funcItems[i]._pShKey;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);

                _funcItems[i] = updatedItem;
            }
        }

        public IntPtr NativePointer { get { return _nativePointer; } }
        public List<FuncItem> Items { get { return _funcItems; } }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IntPtr ptr in _shortCutKeys) Marshal.FreeHGlobal(ptr);
                if (_nativePointer != IntPtr.Zero) Marshal.FreeHGlobal(_nativePointer);
                _disposed = true;
            }
        }
        ~FuncItems()
        {
            Dispose();
        }
    }


    public enum winVer
    {
        WV_UNKNOWN, WV_WIN32S, WV_95, WV_98, WV_ME, WV_NT, WV_W2K,
        WV_XP, WV_S2003, WV_XPX64, WV_VISTA, WV_WIN7, WV_WIN8, WV_WIN81, WV_WIN10
    }


    [Flags]
    public enum DockMgrMsg : uint
    {
        IDB_CLOSE_DOWN = 137,
        IDB_CLOSE_UP                    = 138,
        IDD_CONTAINER_DLG               = 139,

        IDC_TAB_CONT                    = 1027,
        IDC_CLIENT_TAB                  = 1028,
        IDC_BTN_CAPTION                 = 1050,

        DMM_MSG                         = 0x5000,
            DMM_CLOSE                   = (DMM_MSG + 1),
            DMM_DOCK                    = (DMM_MSG + 2),
            DMM_FLOAT                   = (DMM_MSG + 3),
            DMM_DOCKALL                 = (DMM_MSG + 4),
            DMM_FLOATALL                = (DMM_MSG + 5),
            DMM_MOVE                    = (DMM_MSG + 6),
            DMM_UPDATEDISPINFO          = (DMM_MSG + 7),
            DMM_GETIMAGELIST            = (DMM_MSG + 8),
            DMM_GETICONPOS              = (DMM_MSG + 9),
            DMM_DROPDATA                = (DMM_MSG + 10),
            DMM_MOVE_SPLITTER            = (DMM_MSG + 11),
            DMM_CANCEL_MOVE                = (DMM_MSG + 12),
            DMM_LBUTTONUP                = (DMM_MSG + 13),

        DMN_FIRST = 1050,
            DMN_CLOSE                    = (DMN_FIRST + 1),
            //nmhdr.Code = DWORD(DMN_CLOSE, 0));
            //nmhdr.hwndFrom = hwndNpp;
            //nmhdr.IdFrom = ctrlIdNpp;

            DMN_DOCK                    = (DMN_FIRST + 2),
            DMN_FLOAT                    = (DMN_FIRST + 3)
            //nmhdr.Code = DWORD(DMN_XXX, int newContainer);
            //nmhdr.hwndFrom = hwndNpp;
            //nmhdr.IdFrom = ctrlIdNpp;
    }

    /// <summary>
    /// Deprecated since Notepad++ v8.  Use toolbarIconsWithDarkMode instead
    /// (NPPM_ADDTOOLBARICON / NPPM_GETTOOLBARCOUNT are no longer available in NPP 8.x).
    /// Kept for backward compatibility with Notepad++ v7.x.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct toolbarIcons
    {
        public IntPtr hToolbarBmp;
        public IntPtr hToolbarIcon;
    }

    /// <summary>
    /// Structure expected by Notepad++ v8.x as lParam of NPPM_ADDTOOLBARICON_FORDARKMODE.
    /// The plugin must provide a 16x16 monochrome bitmap (hToolbarIconDarkMode)
    /// that adapts to the light/dark mode of Notepad++.
    /// See "toolbarIconsWithDarkMode" in Notepad_plus_msgs.h of Notepad++ v8.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct toolbarIconsWithDarkMode
    {
        public IntPtr hToolbarBmp;                  // HBITMAP: legacy 16x16 color bitmap (kept for compat, may be IntPtr.Zero)
        public IntPtr hToolbarIcon;                 // HICON:   legacy icon (kept for compat, may be IntPtr.Zero)
        public IntPtr hToolbarIconDarkMode;         // HBITMAP: 16x16 monochrome bitmap used in dark mode

        /// <summary>
        /// Builds the structure from bitmaps. All GDI handles created here are
        /// transferred to Notepad++, which takes ownership of them (it destroys
        /// them when the plugin is unloaded / on shutdown).
        /// </summary>
        public static toolbarIconsWithDarkMode Create(Bitmap legacyBmp, Bitmap darkModeMonoGlyph)
        {
            var icons = new toolbarIconsWithDarkMode();
            if (legacyBmp != null)
            {
                icons.hToolbarBmp = legacyBmp.GetHbitmap();
                icons.hToolbarIcon = legacyBmp.GetHicon();
            }
            if (darkModeMonoGlyph != null)
                icons.hToolbarIconDarkMode = darkModeMonoGlyph.GetHbitmap();
            return icons;
        }
    }

    /// <summary>
    /// Helper class to register toolbar icons on both Notepad++ v7.x and v8.x.
    /// On NPP 8.* the old NPPM_ADDTOOLBARICON message was removed; plugins must send
    /// NPPM_ADDTOOLBARICON_FORDARKMODE with a toolbarIconsWithDarkMode structure
    /// containing a 16x16 monochrome glyph (black pixels on transparent background).
    /// </summary>
    public static class ToolbarHelper
    {
        /// <summary>
        /// Converts a color Bitmap into a 16x16 monochrome-compatible 32bpp ARGB bitmap
        /// where all non-transparent colored pixels become solid black. This is the
        /// format expected by the Notepad++ v8.x toolbar (it recolors the glyph itself
        /// depending on the current light/dark theme).
        /// </summary>
        public static Bitmap CreateMonochromeGlyphBitmap(Bitmap source)
        {
            const int size = 16;
            Bitmap mono = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(mono))
            {
                g.Clear(Color.Transparent);
                using (var temp = new Bitmap(size, size))
                {
                    using (Graphics gt = Graphics.FromImage(temp))
                    {
                        gt.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gt.DrawImage(source, 0, 0, size, size);
                    }
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            Color c = temp.GetPixel(x, y);
                            if (c.A > 0 && !(c.R > 240 && c.G > 240 && c.B > 240))
                                mono.SetPixel(x, y, Color.Black);   // NPP will re-color this glyph at runtime
                        }
                    }
                }
            }
            return mono;
        }

        /// <summary>
        /// Adds the given bitmaps to the Notepad++ toolbar for the specified command id.
        /// Automatically picks the correct message/structure for NPP 7.x vs NPP 8.x.
        /// </summary>
        /// <param name="cmdId">funcItem._cmdID of the menu command the icon belongs to</param>
        /// <param name="legacyBmp">color bitmap used by NPP 7.x (and passed through for compat)</param>
        /// <param name="monoGlyph">16x16 monochrome glyph used by NPP 8.x; created from legacyBmp when null</param>
        public static void AddToToolbar(int cmdId, Bitmap legacyBmp, Bitmap monoGlyph = null)
        {
            IntPtr nppHandle = PluginBase.nppData._nppHandle;

            if (monoGlyph == null)
                monoGlyph = CreateMonochromeGlyphBitmap(legacyBmp);

            // --- NPP 8.x path: NPPM_ADDTOOLBARICON_FORDARKMODE (msg + 83) ---
            toolbarIconsWithDarkMode tbIcons8 = toolbarIconsWithDarkMode.Create(legacyBmp, monoGlyph);
            IntPtr pTbIcons8 = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons8));
            try
            {
                Marshal.StructureToPtr(tbIcons8, pTbIcons8, false);
                Win32.SendMessage(nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON_FORDARKMODE, cmdId, pTbIcons8);
            }
            finally
            {
                // only the native memory block is released; the GDI handles inside
                // are now owned by Notepad++ (see toolbarIconsWithDarkMode.Create)
                Marshal.FreeHGlobal(pTbIcons8);
            }
        }
    }
}
