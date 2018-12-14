# SolutionLocalization
Enable automation around resx files in order to participate on Translations pipelines. 

## What are we solving here ?

During development of a C# solution at some point we want to achieve the internationalization of the texts of the entire solution.

And we want more, that these translations can eventually be added dynamically.

For this, what needs to be done is to create Satellite Assemblies for each language and then include these in the distribution of the application.

The natural process is the following:

1) Given all the resx of the solution create some data exchange mechanism (ie: data.xml).

2) These data must be able to be sent in different formats to third parties so they can be translated. (Excel spreadsheets, imported in external tools, etc.)

3) Once translated, we need to create satellite assemblies with them again. Then somebody send us the translated data again and we create the resx from there (eg to have the tracking in a version controller) and then from there create the assemblies.
This step has the complexity of knowing where each of our resx goes in the initial solution.

4) Include the satellite assemblies in our setup.

So let's see what are the tasks we need to follow this path

## MSBuild Tasks for automation

First of all we need to create our "plan" of what we need to translate. In order to do this we have the CreateResourcesCatalog task.

### Create Resource Catalog

Allow to create a catalog of all resources and projects we need to take into account. Basically this task receive the BasePath for the Solution and it will scan looking for C# projects and considering their resources.

#### MSBuild Declaration
```
<ItemGroup>
		<ExcludeDirectory Include="\_TMP"/>
		<ExcludeDirectory Include="\_Build"/>
</ItemGroup>

<Target Name="CreateResourcesCatalog">
		<CreateResourcesCatalog
			BasePath="$(BasePath)"
			SerializedPath="$(SerializedPath)"
			DirectoryExclude="@(ExcludeDirectory)" />
</Target>
```

#### Command Line Call
```
msbuild GenerateResources.msbuild /t:CreateResourcesCatalog /p:BasePath=c:\dev\tilo\  /p:SerializedPath=c:\Dev\Tools\SatelliteGeneration\plan.xml
```

### 

