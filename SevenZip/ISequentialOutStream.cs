﻿using System;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[ComImport]
	[Guid("23170F69-40C1-278A-0000-000300020000")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISequentialOutStream
	{
		[PreserveSig]
		int Write([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data, uint size, IntPtr processedSize);
	}
}
