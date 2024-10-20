# MemPurge

MemPurge is a C# application designed to manage and optimize system memory usage on Windows. It provides functionality to clear the working set of all processes and to clear the file system cache, including the standby list.

## Features

- Clear the working set of all processes.
- Clear the file system cache.
- Support for both 32-bit and 64-bit operating systems.
- Increase privileges to perform memory management tasks.

## Usage

### Clear Working Set of All Processes

The `EmptyAllProcessesWorkingSet` method clears the working set of all processes running on the system.

### Clear File System Cache

The `ClearFileSystemCache` method clears the file system cache. It can also clear the standby list if the `ClearStandbyCache` parameter is set to `true`.

## Code Overview

### Structures

- `SYSTEM_CACHE_INFORMATION`: Structure to hold cache information for 32-bit systems.
- `SYSTEM_CACHE_INFORMATION_64_BIT`: Structure to hold cache information for 64-bit systems.
- `TokPriv1Luid`: Structure to hold privilege information.

### Methods

- `EmptyAllProcessesWorkingSet()`: Clears the working set of all processes.
- `ClearFileSystemCache(bool ClearStandbyCache)`: Clears the file system cache and optionally the standby list.
- `SetIncreasePrivilege(string privilegeName)`: Increases the privilege to perform memory management tasks.
- `IsOS64Bits()`: Checks if the operating system is 64-bit.

### Main Method

The `Main` method calls the `EmptyAllProcessesWorkingSet` and `ClearFileSystemCache` methods to perform memory management tasks.

## How to Run

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Build the solution.
4. Run the application.

## License

This project is licensed under the GPLv3 License. See the LICENSE.txt file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.