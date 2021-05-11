param(
    [Parameter(mandatory=$true)]
    [string]$idCertPassword
)

$newCert = New-SelfSignedCertificate -DnsName "localhost" -FriendlyName "Sitecore Identity Token Signing" -NotAfter (Get-Date).AddYears(5)

Export-PfxCertificate -Cert $newCert -FilePath .\SitecoreIdentityTokenSigning.pfx -Password (ConvertTo-SecureString -String $idCertPassword -Force -AsPlainText)

[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes((Get-Item .\SitecoreIdentityTokenSigning.pfx))) | Out-File -Encoding ascii -NoNewline -Confirm -FilePath .\SitecoreIdentityTokenSigning.txt