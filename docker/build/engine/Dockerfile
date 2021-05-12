# Args
ARG BUILD_IMAGE
ARG RUNTIME_IMAGE

# Build Stage
FROM ${BUILD_IMAGE} as Build
WORKDIR  /app

COPY ./nuget.config ./nuget.config
COPY ./XCDocker.Sample.Solution.sln ./XCDocker.Sample.Solution.sln
COPY ./src/Plugin.Sample.AdventureWorks/Plugin.Sample.AdventureWorks.csproj ./src/Plugin.Sample.AdventureWorks/Plugin.Sample.AdventureWorks.csproj
COPY ./src/Plugin.Sample.Habitat/Plugin.Sample.Habitat.csproj ./src/Plugin.Sample.Habitat/Plugin.Sample.Habitat.csproj
COPY ./src/Plugin.Sample.Payments.Braintree/Plugin.Sample.Payments.Braintree.csproj ./src/Plugin.Sample.Payments.Braintree/Plugin.Sample.Payments.Braintree.csproj
COPY ./src/Sitecore.Commerce.Engine/Sitecore.Commerce.Engine.csproj ./src/Sitecore.Commerce.Engine/Sitecore.Commerce.Engine.csproj
RUN dotnet restore ./XCDocker.Sample.Solution.sln

COPY ./src ./src
RUN dotnet build ./XCDocker.Sample.Solution.sln -c Debug
RUN dotnet publish ./src/Sitecore.Commerce.Engine/Sitecore.Commerce.Engine.csproj -o /app/publish -c Debug

# Runtime Stage
FROM ${RUNTIME_IMAGE} as Runtime
WORKDIR C:/engine
COPY --from=Build /app/publish ./

USER ContainerUser
EXPOSE 5000

ENTRYPOINT ["C:\\LogMonitor\\LogMonitor.exe", "dotnet.exe", "Sitecore.Commerce.Engine.dll"]