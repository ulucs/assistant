module Agent.Executor.Whitelist

open System.Text.RegularExpressions

/// Whitelisted read-only commands
let whitelistedCommands = [
    // File operations (read-only)
    "ls"; "cat"; "head"; "tail"; "grep"; "find"; "tree"; "pwd"; "file"
    // Git (read-only)
    "git status"; "git log"; "git diff"; "git show"; "git branch"
    // System info
    "df"; "du"; "ps"; "whoami"; "uname"; "date"; "uptime"; "hostname"
    // Help/documentation
    "tldr"; "man"; "help"; "which"; "type"; "apropos"
    // Network (read-only)
    "ping"; "traceroute"; "host"; "dig"; "nslookup"
]

/// Check if a command is whitelisted
let isWhitelisted (command: string) : bool =
    let commandLower = command.Trim().ToLower()

    // Check if command starts with any whitelisted command
    whitelistedCommands
    |> List.exists (fun wl -> commandLower.StartsWith(wl))

/// Check if a command contains potentially dangerous patterns
let isDangerous (command: string) : bool =
    let dangerousPatterns = [
        @"rm\s+-rf"           // rm -rf
        @">\s*/dev/sd"        // Writing to disk devices
        @"dd\s+.*of="         // dd output
        @"mkfs"               // Format filesystem
        @":(){.*}:;"          // Fork bomb
        @"chmod.*777"         // Overly permissive permissions
        @"curl.*\|\s*sh"      // Pipe to shell
        @"wget.*\|\s*sh"      // Pipe to shell
        @"sudo"               // Elevated privileges
    ]

    dangerousPatterns
    |> List.exists (fun pattern ->
        Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))

/// Validate a command before execution
let validateCommand (command: string) : Result<unit, string> =
    if System.String.IsNullOrWhiteSpace(command) then
        Error "Command cannot be empty"
    elif isDangerous command then
        Error "Command contains dangerous patterns"
    else
        Ok ()
