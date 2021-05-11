function RemoveNode
{
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $path
    )

    $node = $xml.SelectSingleNode($path)
    if ($node -ne $null)
    {
        $node.ParentNode.RemoveChild($node)
    }
}

(Get-Content -Path .\CommerceOps\CommerceOps.cs) |
    ForEach-Object {$_ -Replace 'global::System.Collections.Generic.IDictionary<string, object> keys', 'global::System.Collections.Generic.Dictionary<string, object> keys'} |
    ForEach-Object {$_ -Replace 'new System.Text.StringBuilder', 'new global::System.Text.StringBuilder'} |
        Set-Content -Path .\CommerceOps\CommerceOps.cs

(Get-Content -Path .\CommerceShops\CommerceShops.cs) |
    ForEach-Object {$_ -Replace 'global::System.Collections.Generic.IDictionary<string, object> keys', 'global::System.Collections.Generic.Dictionary<string, object> keys'} |
        Set-Content -Path .\CommerceShops\CommerceShops.cs

$projectFile = Resolve-Path -Path "..\Sitecore.Commerce.ServiceProxy.csproj"
$xml = [xml](Get-Content $projectFile)

RemoveNode -Path "/Project/ItemGroup/PackageReference[@Include='Microsoft.OData.Core']"
RemoveNode -Path "/Project/ItemGroup/PackageReference[@Include='Microsoft.OData.Edm']"
RemoveNode -Path "/Project/ItemGroup/PackageReference[@Include='Microsoft.Spatial']"

$xml.Save($projectFile)
