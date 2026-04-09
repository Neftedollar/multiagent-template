# PowerShell completions for multiagent-setup
# Add to your PowerShell profile ($PROFILE):
#   . "path\to\tools\completions.ps1"
# Or use the CLI directly:
#   multiagent-setup completions pwsh >> $PROFILE

Register-ArgumentCompleter -Native -CommandName multiagent-setup -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    $tokens = @($commandAst.CommandElements | Select-Object -Skip 1)
    $sub    = if ($tokens.Count -gt 0) { $tokens[0].ToString() } else { "" }

    # Complete subcommand
    if ($tokens.Count -eq 0 -or ($tokens.Count -eq 1 -and -not $sub.StartsWith('-') -and $wordToComplete)) {
        @(
            [pscustomobject]@{ name = 'new';             desc = 'Create a new multi-agent workspace' }
            [pscustomobject]@{ name = 'init';            desc = 'Add workspace files to an existing git repo' }
            [pscustomobject]@{ name = 'add-provider';    desc = 'Add a provider to an existing workspace' }
            [pscustomobject]@{ name = 'remove-provider'; desc = 'Remove a provider from an existing workspace' }
            [pscustomobject]@{ name = 'list-providers';  desc = 'List providers configured in current workspace' }
            [pscustomobject]@{ name = 'update';          desc = 'Update workspace templates to latest version' }
            [pscustomobject]@{ name = 'sync-roles';      desc = 'Sync agent roles to .claude/commands/ (project-local)' }
            [pscustomobject]@{ name = 'install-mcps';    desc = 'Install age-mcp and o-brien MCP servers' }
            [pscustomobject]@{ name = 'hook';            desc = 'Run a built-in hook (cross-platform)' }
            [pscustomobject]@{ name = 'doctor';          desc = 'Check workspace health — tools, files, hooks, roles' }
            [pscustomobject]@{ name = 'completions';     desc = 'Print shell completion script' }
        ) | Where-Object { $_.name -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_.name, $_.name, 'ParameterValue', $_.desc)
        }
        return
    }

    # Complete --provider values when previous token is --provider
    $prevToken = if ($tokens.Count -ge 2) { $tokens[-2].ToString() } else { "" }
    if ($prevToken -eq '--provider') {
        @('claude', 'nessy', 'gemini', 'codex', 'qwen', 'cursor', 'windsurf', 'copilot', 'cline', 'aider', 'continue', 'roo', 'kiro', 'all') |
        Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
        return
    }

    if ($prevToken -eq '--template') {
        @('default', 'saas', 'oss', 'internal') |
        Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
        return
    }

    # Complete flags per subcommand
    switch ($sub) {
        'new' {
            @('--provider', '--template') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
            }
        }
        'init' {
            @('--provider', '--template', '--force') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
            }
        }
        'add-provider' {
            if ($tokens.Count -eq 1 -or ($tokens.Count -eq 2 -and $wordToComplete)) {
                @('claude', 'nessy', 'gemini', 'codex', 'qwen', 'cursor', 'windsurf', 'copilot', 'cline', 'aider', 'continue', 'roo', 'kiro', 'all') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            } else {
                @('--force') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
            }
        }
        'remove-provider' {
            if ($tokens.Count -eq 1 -or ($tokens.Count -eq 2 -and $wordToComplete)) {
                @('nessy', 'gemini', 'codex', 'qwen', 'cursor', 'windsurf', 'copilot', 'cline', 'aider', 'continue', 'roo', 'kiro') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            } else {
                @('--force', '--dry-run') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
            }
        }
        'update' {
            @('--force', '--dry-run') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
            }
        }
        'sync-roles' {
            @('--clone', '--pull', '--global', '--agency-dir') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
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
        'doctor' {
            if ($prevToken -eq '--for') {
                @('sync-roles', 'init', 'update') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            } else {
                @('--for') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
            }
        }
        'completions' {
            @('zsh', 'pwsh') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }
    }
}
