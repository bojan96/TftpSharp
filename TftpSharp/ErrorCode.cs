namespace TftpSharp;
public enum ErrorCode : ushort
{
    Undefined,
    FileNotFound,
    AccessViolation,
    DiskFullOrAllocationExceeded,
    IllegalTftpOperation,
    UnknownTransferId,
    FileAlreadyExists,
    NoSuchUser,
}