msbuild CrudDatastore.Topper.sln /p:Configuration=Release
nuget pack CrudDatastore.Topper/CrudDatastore.Topper.nuspec
nuget pack CrudDatastore.Topper/CrudDatastore.Topper.EntityFramework.nuspec