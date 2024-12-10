using Microsoft.Win32.SafeHandles;

namespace Tmds.Ssh;

internal class LocalResourceReader : IDisposable
{
    private readonly object _source;
    private readonly bool _keepOpen;
    private readonly object _syncLock = new();

    public LocalResourceReader(SafeFileHandle source) : this(source, false)
    {

    }

    public LocalResourceReader(Stream source) : this(source, true)
    {

    }

    private LocalResourceReader(object source, bool keepOpen)
    {
        if (source is not (Stream or SafeFileHandle))
        {
            throw new ArgumentException($"Only {nameof(Stream)} or {nameof(SafeFileHandle)} are allowed.");
        }

        _source = source;
        _keepOpen = keepOpen;
    }

    public long Length => _source switch
    {
        SafeFileHandle fileHandle => RandomAccess.GetLength(fileHandle),
        Stream stream => stream.Length,
        _ => throw new NotImplementedException()
    };

    public int ReadAtOffset(Span<byte> buffer, long offset)
    {
        switch (_source)
        {
            case SafeFileHandle fileHandle:
                return RandomAccess.Read(fileHandle, buffer, offset);
            case Stream stream:
                lock (_syncLock)
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    return stream.Read(buffer);
                }
        }

        throw new NotImplementedException();
    }

    public SafeFileHandle? FileHandle => _source as SafeFileHandle;

    public void Dispose()
    {
        if (_keepOpen) return;
        (_source as IDisposable)?.Dispose();
    }
}
