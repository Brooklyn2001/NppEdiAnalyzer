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
        /// The magic mask color used by the classic Notepad++ toolbar bitmaps: every
        /// pixel that must be transparent is painted pure magenta (RGB 255,0,255).
        /// Notepad++ removes this color when drawing the toolbar / tab icons.
        /// </summary>
        public static readonly Color MagicMagicColor = Color.FromArgb(255, 0, 255);

        /// <summary>
        /// Builds the standard "double sized" bitmap pair expected by Notepad++ v8.x
        /// (see DllInterface::getToolBarList / toolbarIconsWithDarkMode handling in
        /// NPP sources): a 32x32 image containing the 32x32 glyph on top and the
        /// 16x16 glyph below, plus its mask bitmap (same layout, magenta background).
        /// The plugin resources (Fluent-style icons) already store both sizes in one
        /// 32x64 strip, so this method just splits them apart.
        /// </summary>
        /// <param name="strip">32x64 bitmap: [0..31] = 32x32 icon, [32..63] = 16x16 icon</param>
        public static void GetDoubleSizedBitmaps(Bitmap strip, out Bitmap bmp, out Bitmap bmpMask)
        {
            if (strip.Width != 32 || strip.Height != 64)
                throw new ArgumentException("Icon strip must be 32x64 (32x32 icon over 16x16 icon)", "strip");

            bmp = new Bitmap(32, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bmpMask = new Bitmap(32, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(strip, new Rectangle(0, 0, 32, 64), 0, 0, 32, 64, GraphicsUnit.Pixel);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Color c = strip.GetPixel(x, y);
                    bool isBackground = c.ToArgb() == MagicMagicColor.ToArgb();
                    bmpMask.SetPixel(x, y, isBackground ? Color.White : Color.Black);
                }
            }
        }

        /// <summary>
        /// Adds an icon to the Notepad++ toolbar for the specified command id using the
        /// NPP 8.x mechanism (NPPM_ADDTOOLBARICON_FORDARKMODE + toolbarIconsWithDarkMode).
        /// </summary>
        /// <param name="cmdId">funcItem._cmdID of the menu command the icon belongs to</param>
        /// <param name="bmp">32x64 double-sized color bitmap (32x32 over 16x16)</param>
        /// <param name="bmpMask">32x64 mask bitmap for bmp (white = background)</param>
        public static void AddToToolbar(int cmdId, Bitmap bmp, Bitmap bmpMask)
        {
            IntPtr nppHandle = PluginBase.nppData._nppHandle;

            var tbIcons = new toolbarIconsWithDarkMode();
            tbIcons.hToolbarBmp = bmp.GetHbitmap();           // ownership passed to Notepad++
            tbIcons.hToolbarIcon = IntPtr.Zero;               // legacy HICON not needed on NPP 8.x
            tbIcons.hToolbarIconDarkMode = bmpMask.GetHbitmap(); // ownership passed to Notepad++

            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            try
            {
                Marshal.StructureToPtr(tbIcons, pTbIcons, false);
                Win32.SendMessage(nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON_FORDARKMODE, cmdId, pTbIcons);
            }
            finally
            {
                // only the native memory block is released; the GDI handles inside
                // are now owned by Notepad++ (it destroys them on plugin unload/shutdown)
                Marshal.FreeHGlobal(pTbIcons);
            }
        }
    }
}
