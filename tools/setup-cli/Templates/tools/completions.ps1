# PowerShell completions for multiagent-setup
# Add to your PowerShell profile ($PROFILE):
#   . "path\to\tools\completions.ps1"

Register-ArgumentCompleter -Native -CommandName multiagent-setup -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    $tokens = @($commandAst.CommandElements | Select-Object -Skip 1)
    $sub    = if ($tokens.Count -gt 0) { $tokens[0].ToString() } else { "" }

    # Complete subcommand
    if ($tokens.Count -eq 0 -or ($tokens.Count -eq 1 -and -not $sub.StartsWith('-') -and $wordToComplete)) {
        @(
            [pscustomobject]@{ name = 'new';          desc = 'Create a new multi-agent workspace' }
            [pscustomobject]@{ name = 'add-provider'; desc = 'Add a provider to an existing workspace' }
            [pscustomobject]@{ name = 'sync-roles';   desc = 'Sync agent roles to .claude/commands/ (project-local)' }
            [pscustomobject]@{ name = 'install-mcps'; desc = 'Install age-mcp and o-brien MCP servers' }
            [pscustomobject]@{ name = 'hook';         desc = 'Run a built-in hook (cross-platform)' }
        ) | Where-Object { $_.name -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_.name, $_.name, 'ParameterValue', $_.desc)
        }
        return
    }

    # Complete flags per subcommand
    switch ($sub) {
        'new' {
            if ($tokens.Count -le 2) {
                @('--provider') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
            } elseif ($tokens[-2] -eq '--provider') {
                @('claude', 'nessy', 'gemini', 'codex', 'qwen', 'all') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            }
        }
        'add-provider' {
            if ($tokens.Count -eq 1 -or ($tokens.Count -eq 2 -and $wordToComplete)) {
                @('claude', 'nessy', 'gemini', 'codex', 'qwen', 'all') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            } else {
                @('--force') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
            }
        }
        'sync-roles' {
            @('--clone', '--pull', '--agency-dir', '--workspace-root') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
            }
        }
        'install-mcps' {
            @('--docker', '--manual', '--age-conn', '--obrien-conn', '--target') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
            }
        }
        'hook' {
            @('block-dangerous', 'enforce-commit-msg', 'auto-lint', 'log-agent', 'stop-guard', 'research-reminder') |
            Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }
    }
}
