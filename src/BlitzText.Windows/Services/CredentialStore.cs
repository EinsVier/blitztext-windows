using System.Runtime.InteropServices;
using System.Text;

namespace BlitzText.Windows.Services;

public sealed class CredentialStore
{
    private const string OpenAiApiKeyTarget = "BlitzText.OpenAI.ApiKey";
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public string ReadOpenAiApiKey()
    {
        return ReadSecret(OpenAiApiKeyTarget);
    }

    public void SaveOpenAiApiKey(string apiKey)
    {
        SaveSecret(OpenAiApiKeyTarget, "OpenAI API Key", apiKey);
    }

    private static string ReadSecret(string target)
    {
        if (!CredRead(target, CredTypeGeneric, 0, out var credentialPointer))
        {
            return "";
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(credentialPointer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
            {
                return "";
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    private static void SaveSecret(string target, string userName, string secret)
    {
        var secretBytes = Encoding.Unicode.GetBytes(secret);
        var secretPointer = Marshal.AllocCoTaskMem(secretBytes.Length);

        try
        {
            Marshal.Copy(secretBytes, 0, secretPointer, secretBytes.Length);

            var credential = new Credential
            {
                Type = CredTypeGeneric,
                TargetName = target,
                CredentialBlobSize = secretBytes.Length,
                CredentialBlob = secretPointer,
                Persist = CredPersistLocalMachine,
                UserName = userName
            };

            if (!CredWrite(ref credential, 0))
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Could not save credential '{target}'. Windows error: {error}.");
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(secretPointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPointer);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref Credential userCredential, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);
}
