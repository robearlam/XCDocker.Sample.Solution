# XCDocker.Sample.Solution
A solution for customising a Sitecore Experience Commerce solution, running inside of Docker Containers.
This is running Sitecore Experience Commerce 10.1

## Pre-Installation Steps

- Download `Packages for On Premises WDP 2021.02-7.0.162` & `Sitecore Commerce Container SDK` from <a href="https://dev.sitecore.net/Downloads/Sitecore_Commerce/101/Sitecore_Experience_Commerce_101.aspx">Sitecore Downloads</a>.
- Extract both archives to a location on disk, you will need this for the params later.
- Within the 'Sitecore.Commerce.WDP.2021.02-7.0.162' archive, also extract the 'Sitecore.Commerce.Engine.SDK.7.0.55' contained within it.
- Download & install <a href="https://www.postman.com/">Postman</a>.
- Import the postman `devops` & `shops` request collections from the `Sitecore.Commerce.WDP.2021.02-7.0.162` extract previously into Postman.
- Import the postman `Containers - Docker - Habitat.postman_environment` environment configuration from within the `Sitecore.Commerce.Container.SDK.2.0.159` extracted previously into Postman.

## Local Setup using Docker

- Ensure docker is running in 'Windows Containers Mode'
- Ensure the following commands are run from an elevated PS Terminal

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

### Stand up docker containers with the following commands

```
docker-compose up -d
```

### Bootstrap & Initialise Engine

- In Postman, ensure you have the `Containers - Docker - Habitat` selected.
- Execute the `Authentication -> Sitecore -> GetToken` command.
- Execute the `SitecoreCommerce_DevOps -> 1 Environment Bootstrap -> Bootstrap Sitecore Commerce` command.
- Execute the `SitecoreCommerce_DevOps -> 3 Environment Initialize -> Ensure/Sync default content paths` command.
- Execute the `SitecoreCommerce_DevOps -> 3 Environment Initialize -> Initialize Environment` command.
- Execute the `SitecoreCommerce_DevOps -> 3 Environment Initialize -> Check Long Running Command Status` command to wait for confirmation the `Initialize Environment` has completed.

### Populate Hosts File

Populate your hosts file with the following entries

127.0.0.1 xc1cd.localhost
127.0.0.1 xc1cm.localhost
127.0.0.1 xc1id.localhost
127.0.0.1 bizfx.localhost
127.0.0.1 authoring.localhost
127.0.0.1 shops.localhost
127.0.0.1 ops.localhost

### Configure Catalog Templates

- Log into Sitecore CMS at <a href="https://xc1cm.localhost/sitecore">https://xc1cm.localhost/sitecore</a>
    - Username: admin
    - Password: Password12345
- Open Content Editor
- Click `Commerce` tab
- Click `Refresh Commerce Cache`
- Click `Update Data Templates`

### Rebuild XC Indexes

- In Postman, ensure you have the `Containers - Docker - Habitat` selected.
- Execute the `SitecoreCommerce_DevOps -> Minions -> Run FullIndex Minion - Customers` command.
- Execute the `SitecoreCommerce_DevOps -> Minions -> Run FullIndex Minion - PriceCards` command.
- Execute the `SitecoreCommerce_DevOps -> Minions -> Run FullIndex Minion - Promotions` command.

### Configure XP Indexes

- Log into Sitecore CMS at <a href="https://xc1cm.localhost/sitecore">https://xc1cm.localhost/sitecore</a>.
- Load `Control Panel`.
- Select `Populated Solr Managed Schema`.
- Ensure all checkboxes are checked.
- Click `Populate`.
- When complete click `Close`.
- Click 'Indexing Manager'.
- Ensure all checkboxes are checked.
- Click `Rebuild`, (Note, this can be a slow process to complete).
- When complete click `Close`.

### Create Tenant & Storefront

- Follow steps <a href="https://doc.sitecore.com/developers/101/sitecore-experience-commerce/en/create-a-commerce-tenant-and-site.html">here</a> to create the default Storefront & Tenant.
- Perform a full database publish.
- Perform a rebuild of the `Sitecore_master_index`, `Sitecore_web_index`,`Sitecore_sxa_master_index`, & `Sitecore_sxa_web_index` from the `indexing manager` in the `control panel` as in the step above.