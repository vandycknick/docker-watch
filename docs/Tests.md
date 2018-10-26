# Running unit tests

## Run tests and create code coverage file

```sh
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput='../../lcov.info' ./test/DockerWatch.Test/DockerWatch.Test.csproj
```
