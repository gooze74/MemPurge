using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace MemPurge
{
    //SYSTEM_CACHE_INFORMATION
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SYSTEM_CACHE_INFORMATION
    {
        public uint CurrentSize;
        public uint PeakSize;
        public uint PageFaultCount;
        public uint MinimumWorkingSet;
        public uint MaximumWorkingSet;
        public uint Unused1;
        public uint Unused2;
        public uint Unused3;
        public uint Unused4;
    }

    //SYSTEM_CACHE_INFORMATION_64_BIT
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SYSTEM_CACHE_INFORMATION_64_BIT
    {
        public long CurrentSize;
        public long PeakSize;
        public long PageFaultCount;
        public long MinimumWorkingSet;
        public long MaximumWorkingSet;
        public long Unused1;
        public long Unused2;
        public long Unused3;
        public long Unused4;
    }

    //TokPriv1Luid
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TokPriv1Luid
    {
        public int Count;
        public long Luid;
        public int Attr;
    }
    public class Program
    {
        const int SE_PRIVILEGE_ENABLED = 2;
        const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        const int SystemFileCacheInformation = 0x0015;
        const int SystemMemoryListInformation = 0x0050;
        const int MemoryPurgeStandbyList = 4;
        const int MemoryEmptyWorkingSets = 2;

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("ntdll.dll")]
        public static extern UInt32 NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        static void EmptyAllProcessesWorkingSet()
        {
            Process[] allProcesses = Process.GetProcesses();
            for (int i = 0; i < allProcesses.Length; i++)
            {
                Process p = allProcesses[i];
                try
                {
                    EmptyWorkingSet(p.Handle);
                }
                catch (Exception ex)
                {
                    // Some processes may not be able to have their working set emptied like system processes
                }
            }
        }

        static Func<bool> IsOS64Bits = () => Marshal.SizeOf(typeof(IntPtr)) == 8;

        static void ClearFileSystemCache(bool ClearStandbyCache)
        {
            try
            {
                //Check if privilege can be increased
                if (SetIncreasePrivilege(SE_INCREASE_QUOTA_NAME))
                {
                    uint errorCode;
                    if (!IsOS64Bits())
                    {
                        SYSTEM_CACHE_INFORMATION cacheInformation = new SYSTEM_CACHE_INFORMATION();
                        cacheInformation.MinimumWorkingSet = uint.MaxValue;
                        cacheInformation.MaximumWorkingSet = uint.MaxValue;
                        int SystemInfoLength = Marshal.SizeOf(cacheInformation);
                        GCHandle gcHandle = GCHandle.Alloc(cacheInformation, GCHandleType.Pinned);
                        errorCode = NtSetSystemInformation(SystemFileCacheInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                        gcHandle.Free();
                    }
                    else
                    {
                        SYSTEM_CACHE_INFORMATION_64_BIT cacheInformation = new SYSTEM_CACHE_INFORMATION_64_BIT();
                        cacheInformation.MinimumWorkingSet = -1L;
                        cacheInformation.MaximumWorkingSet = -1L;
                        int SystemInfoLength = Marshal.SizeOf(cacheInformation);
                        GCHandle gcHandle = GCHandle.Alloc(cacheInformation, GCHandleType.Pinned);
                        errorCode = NtSetSystemInformation(SystemFileCacheInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                        gcHandle.Free();
                    }
                    if (errorCode != 0)
                        throw new Exception("NtSetSystemInformation(SYSTEMCACHEINFORMATION) error: ", new Win32Exception(Marshal.GetLastWin32Error()));
                }

                //If passes paramater is 'true' and the privilege can be increased, then clear standby lists through MemoryPurgeStandbyList
                if (ClearStandbyCache && SetIncreasePrivilege(SE_PROFILE_SINGLE_PROCESS_NAME))
                {
                    int SystemInfoLength = Marshal.SizeOf(MemoryPurgeStandbyList);
                    GCHandle gcHandle = GCHandle.Alloc(MemoryPurgeStandbyList, GCHandleType.Pinned);
                    uint errorCode = NtSetSystemInformation(SystemMemoryListInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                    gcHandle.Free();
                    if (errorCode != 0)
                        throw new Exception("NtSetSystemInformation(SYSTEMMEMORYLISTINFORMATION) error: ", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        //Function to increase Privilege, returns boolean
        static bool SetIncreasePrivilege(string privilegeName)
        {
            using (WindowsIdentity current = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges))
            {
                TokPriv1Luid newst;
                newst.Count = 1;
                newst.Luid = 0L;
                newst.Attr = SE_PRIVILEGE_ENABLED;

                //Retrieves the LUID used on a specified system to locally represent the specified privilege name
                if (!LookupPrivilegeValue(null, privilegeName, ref newst.Luid))
                    throw new Exception("Error in LookupPrivilegeValue: ", new Win32Exception(Marshal.GetLastWin32Error()));

                //Enables or disables privileges in a specified access token
                int errorCode = AdjustTokenPrivileges(current.Token, false, ref newst, 0, IntPtr.Zero, IntPtr.Zero) ? 1 : 0;
                if (errorCode == 0)
                    throw new Exception("Error in AdjustTokenPrivileges: ", new Win32Exception(Marshal.GetLastWin32Error()));
                return errorCode != 0;
            }
        }

        static void Main(string[] args)
        {
            //Clear working set of all processes
            EmptyAllProcessesWorkingSet();

            //Clear file system cache
            ClearFileSystemCache(true);
        }
    }
}