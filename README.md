# XCDocker.Sample.Solution
A solution for customising a Sitecore Experience Commerce solution, running inside of Docker Containers.

## Pre-Installation Steps

Extract the `Sitecore.Commerce.Container.SDK.2.0.159` archive to a location on disk, you will need this for the params later

These need to be run from an elevated PS Terminal

### Generate ID Cert
Run `./scripts/CreateIdServerCert.ps1 -idCertPassword <<ID_CERT_PASSWORD>>`

### Generate TLS Certs
Run `./scripts/GenerateCerts.ps1`

### Populate .env file
Run 

```
./scripts/PopulateEnv.ps1 `
    -licenseFilePath <<PATH_TO_YOUR_LICENSE_FILE>> `
    -braintreeEnvironment <<BRAINTREE_ENVIRONMENT_ID>> `
    -braintreeMerchantId <<BRAINTREE_MERCHANT_ID>> `
    -braintreePublicKey <<BRAINTREE_PUBLIC_KEY>> `
    -braintreePrivateKey <<BRAINTREE_PRIVATE_KEY>> `
    -telerikKey <<64_TO_128_CHAR_RANDOM_STRING>> `
    -idCert <<CONTENTS_OF_SitecoreIdentityTokenSigning.txt_GENERATE_BY_ID_SERVER_SCRIPT_ABOVE>> `
    -idSecret <<64_CHAR_RANDOM_STRING>> `
    -idPassword <<ID_CERT_PASSWORD_USED_ABOVE>> `
    -xcIdSecret <<64_CHAR_RANDOM_STRING>> `
    -reportingApiKey <<32_CHAR_RANDOM_STRING>>
```