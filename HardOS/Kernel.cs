using Cosmos.Core.Memory;
using Cosmos.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sys = Cosmos.System;

namespace HardOS
{
    public class Process
    {
        public string ExecutablePath { get; set; }
        public bool IsRunning { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


    public class Kernel : Sys.Kernel
    {
        private Sys.FileSystem.CosmosVFS fs;
        private string currentDirectory = @"0:\";
        private List<Process> processList = new List<Process>();
        private List<User> users = new List<User>();
        private User loggedInUser;

        protected override void BeforeRun()
        {
            Console.WriteLine("                            __       ______    ______  ");
            Console.WriteLine("                           |  \\    /      \\ /      \\");
            Console.WriteLine("       __  ______   ______ _| $$_  |  $$$$$$|  $$$$$$\\\r\n");
            Console.WriteLine("      |  \\|      \\ /      |   $$ \\ | $$  | $| $$___\\$$");
            Console.WriteLine("      \\$$ \\$$$$$$|  $$$$$$\\$$$$$$ | $$  | $$\\$$    \\ ");
            Console.WriteLine("     |  \\/      $| $$   \\$$| $$ __| $$  | $$_\\$$$$$$\\");
            Console.WriteLine("     | $|  $$$$$$| $$      | $$|  | $$__/ $|  \\__| $$");
            Console.WriteLine("     | $$\\$$    $| $$       \\$$  $$\\$$    $$\\$$    $$");
            Console.WriteLine("__   | $$ \\$$$$$$$\\$$        \\$$$$  \\$$$$$$  \\$$$$$$ ");
            Console.WriteLine("|  \\__/ $$  ");
            Console.WriteLine(" \\$$    $$  ");
            Console.WriteLine("|  \\$$$$$$   ");
            Console.WriteLine("Cosmos booted successfully. Type a line of text to get it echoed back.");
            Console.WriteLine("Welcome to JartOS ");
            Console.WriteLine("LOGIN TO PROCEED : ");

            // Add some initial users (for demonstration purposes)
            users.Add(new User { Username = "jonel", Password = "jonel123" });

            // Login loop
            while (true)
            {
                Console.Write("Enter your username: ");
                string username = Console.ReadLine();

                Console.Write("Enter your password: ");
                string password = Console.ReadLine();

                var user = users.Find(u => u.Username == username && u.Password == password);

                if (user != null)
                {
                    loggedInUser = user;
                    Console.WriteLine($"Login successful. Welcome, {username}!");
                    break; // Exit the login loop
                }
                else
                {
                    Console.WriteLine("Login failed. Invalid username or password. Try again.");
                }
            }

            fs = new Sys.FileSystem.CosmosVFS();
            Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        }

        protected override void Run()
        {
            Console.WriteLine($"Welcome to the terminal! Type 'help' for a list of commands.");


            while (true)
            {
                Console.Write($"{currentDirectory}> ");
                var input = Console.ReadLine();

                // Handle commands
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                var commandParts = input.Split(' ');
                var command = commandParts[0].ToLower();

                switch (command)
                {
                    case "dir":
                        ListFilesInDirectory(currentDirectory);
                        break;
                    case "mkdir":
                        if (commandParts.Length > 1)
                        {
                            CreateDirectory(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: mkdir [directoryName]");
                        }
                        break;
                    case "rmdir":
                        if (commandParts.Length > 1)
                        {
                            RemoveDirectory(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: rmdir [directoryName]");
                        }
                        break;
                    case "cd":
                        if (commandParts.Length > 1)
                        {
                            ChangeDirectory(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: cd [directoryName]");
                        }
                        break;
                    case "echo":
                        if (commandParts.Length > 2 && commandParts[1] == ">")
                        {
                            CreateFile(commandParts[2], string.Join(" ", commandParts.Skip(3)));
                        }
                        else
                        {
                            Console.WriteLine("Usage: echo > [fileName] [content]");
                        }
                        break;
                    case "type":
                        if (commandParts.Length > 1)
                        {
                            ViewFileContents(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: type [fileName]");
                        }
                        break;
                    case "delete":
                        if (commandParts.Length > 1)
                        {
                            DeleteFile(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: delete [fileName]");
                        }
                        break;
                    case "ramspace":
                        ShowAvailableRamSpace(currentDirectory);
                        break;
                    case "memoryspace":
                        ShowAvailableFreeSpace(currentDirectory);
                        break;
                    case "collect":
                        CollectGarbage();
                        break;
                    case "run":
                        if (commandParts.Length > 1)
                        {
                            RunExecutable(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: run [executable]");
                        }
                        break;
                    case "list":
                        ListProcesses();
                        break;
                    case "terminate":
                        if (commandParts.Length > 1)
                        {
                            TerminateProcess(commandParts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: terminate [processId]");
                        }
                        break;
                    case "exit":
                        Sys.Power.Shutdown();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    default:
                        Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                        break;
                }
            }
        }


        private void ListFilesInDirectory(string directoryPath)
        {
            try
            {
                Console.WriteLine($"Contents of directory {directoryPath}:");

                // Display directories
                var directoriesList = Directory.GetDirectories(directoryPath);
                foreach (var directory in directoriesList)
                {
                    Console.WriteLine($"[DIR] {Path.GetFileName(directory)}");
                }

                // Display files
                var filesList = Directory.GetFiles(directoryPath);
                foreach (var file in filesList)
                {
                    Console.WriteLine($"[FILE] {Path.GetFileName(file)}");
                }

                ShowAvailableFreeSpace(directoryPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files: {ex.Message}");
            }
        }

        private void ShowAvailableRamSpace(string directoryPath)
        {
            try
            {
                var availableSpaceBytes = GCImplementation.GetAvailableRAM(); // Use Cosmos memory management method
                var usedSpaceRam = GCImplementation.GetUsedRAM();
                Console.WriteLine($"Available Ram Space in '{directoryPath}': {availableSpaceBytes} MB");
                Console.WriteLine($"Used Ram Space : {usedSpaceRam} bit");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available free space: {ex.Message}");
            }
        }

        private void ShowAvailableFreeSpace(string directoryPath)
        {
            try
            {
                var availableSpace = fs.GetAvailableFreeSpace(directoryPath);
                Console.WriteLine("Available Free Space: " + availableSpace);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available free space: {ex.Message}");
            }
        }

        private void CreateDirectory(string directoryName)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(currentDirectory, directoryName));
                Console.WriteLine($"Directory '{directoryName}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
            }
        }

        private void RemoveDirectory(string directoryName)
        {
            try
            {
                Directory.Delete(Path.Combine(currentDirectory, directoryName), true);
                Console.WriteLine($"Directory '{directoryName}' removed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing directory: {ex.Message}");
            }
        }

        private void ChangeDirectory(string newDirectory)
        {
            try
            {
                if (newDirectory == "..")
                {
                    currentDirectory = Path.GetDirectoryName(currentDirectory.TrimEnd(Path.DirectorySeparatorChar));
                }
                else
                {
                    if (Directory.Exists(Path.Combine(currentDirectory, newDirectory)))
                    {
                        currentDirectory = Path.Combine(currentDirectory, newDirectory);
                    }
                    else
                    {
                        Console.WriteLine($"Directory '{newDirectory}' does not exist.");
                    }
                }

                Console.WriteLine($"Current directory: {currentDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing directory: {ex.Message}");
            }
        }

        private void CreateFile(string fileName, string content)
        {
            try
            {
                File.WriteAllText(Path.Combine(currentDirectory, fileName), content);
                Console.WriteLine($"File '{fileName}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file: {ex.Message}");
            }
        }

        private void ViewFileContents(string fileName)
        {
            try
            {
                var filePath = Path.Combine(currentDirectory, fileName);
                if (File.Exists(filePath))
                {
                    var fileContent = File.ReadAllText(filePath);
                    Console.WriteLine($"File contents of '{fileName}':");
                    Console.WriteLine(fileContent);
                }
                else
                {
                    Console.WriteLine($"File '{fileName}' does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error viewing file contents: {ex.Message}");
            }
        }

        private void CollectGarbage()
        {
            try
            {
                int freedObjectsCount = Heap.Collect(); // Use Cosmos memory management method
                Console.WriteLine($"Garbage collection completed. Freed {freedObjectsCount} objects.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during garbage collection: {ex.Message}");
            }
        }






        private void RunExecutable(string executablePath)
        {
            try
            {
                if (File.Exists(Path.Combine(currentDirectory, executablePath)) && executablePath.EndsWith(".exe"))
                {
                    var newProcess = new Process { ExecutablePath = executablePath, IsRunning = true };
                    processList.Add(newProcess);
                    Console.WriteLine($"Process '{executablePath}' started with Process ID: {processList.IndexOf(newProcess)}");
                }
                else
                {
                    Console.WriteLine($"Executable '{executablePath}' not found or not supported.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running executable: {ex.Message}");
            }
        }

        private void TerminateProcess(string processIdString)
        {
            try
            {
                if (int.TryParse(processIdString, out int processId) && processId >= 0 && processId < processList.Count)
                {
                    var terminatedProcess = processList[processId];
                    terminatedProcess.IsRunning = false;
                    processList.Remove(terminatedProcess);  
                    Console.WriteLine($"Process '{terminatedProcess.ExecutablePath}' with Process ID {processId} terminated.");
                }
                else
                {
                    Console.WriteLine($"Invalid process ID: {processIdString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error terminating process: {ex.Message}");
            }
        }




        private void ListProcesses()
        {
            Console.WriteLine("Running Processes:");
            if (processList.Count > 0)
            {
                foreach (var process in processList)
                {
                    Console.WriteLine($"- Process: {process.ExecutablePath}, PID: {processList.IndexOf(process)}");
                }
            }
            else
            {
                Console.WriteLine("No processes are currently running.");
            }
        }

        private void DeleteFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(currentDirectory, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"File '{fileName}' deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"File '{fileName}' does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }


        private void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("dir          - List files and directories in the current directory");
            Console.WriteLine("memoryspace  - Show available free space");
            Console.WriteLine("ramspace     - Show available free space of RAM");
            Console.WriteLine("mkdir        - Create a new directory");
            Console.WriteLine("rmdir        - Remove a directory");
            Console.WriteLine("cd           - Change the current directory");
            Console.WriteLine("echo         - Create a new file");
            Console.WriteLine("type         - View file contents");
            Console.WriteLine("delete       - delete existing file in current directory");
            Console.WriteLine("run          - Run an executable");
            Console.WriteLine("terminate    - Terminate a running process");
            Console.WriteLine("list         - List running processes");
            Console.WriteLine("collect      - Perform garbage collection");
            Console.WriteLine("exit         - Shutdown the system");
            Console.WriteLine("help         - Show available commands");
        }
    }
}
