<#
.SYNOPSIS
Searches a file index for fast file and folder searches.

.DESCRIPTION
The Search-Everything cmdlet uses VoidTools' Everything Search program to locate files and folders by name.
Since Everything Search maintains an index of files, searches will perform much faster than when using Get-ChildItem.

.PARAMETER Filter
The filter string in the same format that the Everything Search GUI uses. Details can be found at http://www.voidtools.com/support/everything/searching/

.PARAMETER MatchPath
Enables matching the filter against the file path instead of just the file name. This will impact performance.

.PARAMETER MatchCase
Matches the filter with exact casing.

.PARAMETER MatchWholeWord
Only matches the filter against entire words instead of allowing substring matches.

.PARAMETER MatchAccents
Matches accented characters in the filter instead of ignoring accent marks.

.PARAMETER Regex
Treat the filter as a regular expression. Supported operators are listed at http://www.voidtools.com/support/everything/searching/#regex

.NOTES
The objects returned can be differentiated as files and folders by their types FileInfo and DirectoryInfo.
#>
function Search-Everything {
    [CmdletBinding()]
    param(
        [Parameter(
            Mandatory=$True,
            ValueFromPipeline=$True,
            ValueFromPipelineByPropertyName=$True,
            Position=0)]
        [string] $Filter,
        [switch] $MatchPath,
        [switch] $MatchCase,
        [switch] $MatchWholeWord,
        [switch] $MatchAccents,
        [switch] $Regex
    )

    BEGIN {
        $everything = New-Object 'PSEverything.Everything'
    }

    PROCESS {
        $everything.MatchPath = $MatchPath
        $everything.MatchAccents = $MatchAccents
        $everything.MatchCase = $MatchCase
        $everything.MatchWholeWord = $MatchWholeWord
        $everything.Regex = $Regex

        Write-Output $everything.Search($Filter)
    }

    END {
        $everything.DestroyHandle()
        $everything = $null
    }
}