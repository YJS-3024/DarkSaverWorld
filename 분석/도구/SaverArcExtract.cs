using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

internal static class SaverArcExtract
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string path);

    [DllImport("Compress.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool OpenArcFile(string path);

    [DllImport("Compress.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool UnpackAFile(string name);

    [DllImport("Compress.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetPointer();

    [DllImport("Compress.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetBuf2Size();

    [DllImport("Compress.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void CloseArcFile();

    private static int Main(string[] args)
    {
        if (args.Length != 3 && args.Length != 4)
        {
            Console.Error.WriteLine("Usage: SaverArcExtract <Compress.dll directory> <archive> <output directory>");
            Console.Error.WriteLine("   or: SaverArcExtract <Compress.dll directory> <archive> <entry> <output>");
            return 2;
        }

        string dllDirectory = Path.GetFullPath(args[0]);
        string archive = Path.GetFullPath(args[1]);
        string output = Path.GetFullPath(args[args.Length - 1]);
        List<string> allEntries = args.Length == 3
            ? new List<string>(ReadEntryNames(archive))
            : null;

        if (!SetDllDirectory(dllDirectory))
            throw new InvalidOperationException("SetDllDirectory failed: " + Marshal.GetLastWin32Error());

        if (!OpenArcFile(archive))
        {
            Console.Error.WriteLine("OpenArcFile failed.");
            return 3;
        }

        try
        {
            if (args.Length == 4)
            {
                if (!ExtractEntry(args[2], output))
                    return 4;
                return 0;
            }

            int count = 0;
            foreach (string entry in allEntries)
            {
                string safeName = Path.GetFileName(entry);
                if (!ExtractEntry(entry, Path.Combine(output, safeName)))
                    return 4;
                count++;
            }
            Console.WriteLine("Extracted {0} entries: {1}", count, output);
            return 0;
        }
        finally
        {
            CloseArcFile();
        }
    }

    private static bool ExtractEntry(string entry, string output)
    {
        if (!UnpackAFile(entry))
        {
            Console.Error.WriteLine("UnpackAFile failed: " + entry);
            return false;
        }

        int size = GetBuf2Size();
        IntPtr pointer = GetPointer();
        if (size < 0 || (size > 0 && pointer == IntPtr.Zero))
            throw new InvalidDataException("Invalid unpacked buffer: " + entry);

        byte[] data = new byte[size];
        if (size > 0)
            Marshal.Copy(pointer, data, 0, size);

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllBytes(output, data);
        return true;
    }

    private static IEnumerable<string> ReadEntryNames(string archive)
    {
        using (FileStream stream = File.OpenRead(archive))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            int versionLength = reader.ReadByte();
            reader.ReadBytes(versionLength);

            while (stream.Position + 36 <= stream.Length)
            {
                byte[] record = reader.ReadBytes(36);
                int nameLength = record[0];
                if (nameLength > 16)
                    throw new InvalidDataException("Invalid ARC entry name length.");

                string name = System.Text.Encoding.ASCII.GetString(record, 1, nameLength);
                if (name == "HEADEND")
                    yield break;
                yield return name;
            }
        }
        throw new InvalidDataException("ARC HEADEND record was not found.");
    }
}
