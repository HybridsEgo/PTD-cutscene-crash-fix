using Memory;
using System.Diagnostics;

class Program
{
    public string mccProcessSteam = "MCC-Win64-Shipping";
    public string mccProcessWinstore = "MCCWinStore-Win64-Shipping";

    //
    // Pointers for ODSTMCC 1.3495.0.0
    public string Revert = "halo3odst.dll+0x20FF6BF";
    public string Tick = "halo3odst.dll+0x01FE9630,0x10C";
    public string MapHeader = "halo3odst.dll+0x20EF068";
    public string MapNameOffset = "halo3odst.dll+0x20EF155";
    //

    public async Task CheckForC100()
    {
        while (true)
        {
            var HeaderCheck = memory.ReadBytes(MapHeader, 4);
            string HeaderString = (HeaderCheck != null && HeaderCheck.Length > 0) ? System.Text.Encoding.UTF8.GetString(HeaderCheck) : string.Empty;

            if (HeaderString == "daeh")
            {
                var C100IsLoaded = memory.ReadBytes(MapNameOffset, 9);
                string MapNameOffsetString = (C100IsLoaded != null && C100IsLoaded.Length > 0) ? System.Text.Encoding.UTF8.GetString(C100IsLoaded) : string.Empty;

                if (MapNameOffsetString == @"c100\c100")
                {
                    var GetTick = memory.ReadInt(Tick);

                    if (GetTick >= 2) 
                    {
                        memory.WriteMemory(Revert, "byte", "0x01");
                        Console.WriteLine("REVERT!");
                        await Task.Delay(25000);
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    #region Memory & Process

    public Mem memory = new Mem();
    private Process p;

    private string selectedProcessName;
    bool startup = false;

    public async Task GetProcess()
    {
        try
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                if (process.ProcessName.Equals(mccProcessSteam, StringComparison.OrdinalIgnoreCase))
                {
                    selectedProcessName = mccProcessSteam;
                    break;
                }
                else if (process.ProcessName.Equals(mccProcessWinstore, StringComparison.OrdinalIgnoreCase))
                {
                    selectedProcessName = mccProcessWinstore;
                    break;
                }
            }

            if (string.IsNullOrEmpty(selectedProcessName))
            {
                Console.WriteLine("No MCC process found. Please ensure MCC is running.");
                return;
            }

            p = Process.GetProcessesByName(selectedProcessName)[0];
            memory.OpenProcess(p.Id);

            if (memory == null || memory.theProc == null || selectedProcessName == null)
            {
                Console.WriteLine("Failed to open memory process.");
                return;
            }

            if (!startup)
            {
                Console.WriteLine($"Found: {selectedProcessName} (ID: {p.Id})");
            }

            memory.theProc.Refresh();
            memory.modules.Clear();

            foreach (ProcessModule module in memory.theProc.Modules)
            {
                if (!string.IsNullOrEmpty(module.ModuleName) && !memory.modules.ContainsKey(module.ModuleName))
                {
                    memory.modules.Add(module.ModuleName, module.BaseAddress);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("The MCC process was not found... Please open MCC and try again.");
        }
    }

    #endregion

    static async Task Main()
    {
        Program program = new Program();
        await program.GetProcess();

        while (true)
        {
            await program.CheckForC100();
        }
    }
}
