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

### Create Resource Catalog Task

Allow to create a catalog of all resources and projects we need to take into account. Basically this task receive the BasePath for the Solution and it will scan looking for C# projects and considering their resources.

#### MSBuild Declaration
```
<UsingTask AssemblyFile="SolutionLocalization.dll" TaskName="SolutionLocalization.CreateResourcesCatalog" />

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

Now that we have a plan we need to create a way to interchange information with others. Instead on sending all the resx file we defined a simple unified of sending all the messages taking into account all the translation of a given message.
Basically the format is a collection of Messages with this format:

```
<Message>
	<ResourceFile>Architecture\Base\Common\Resources\Messages.resx</ResourceFile>
	<ResourceKey>ActionNotDefined</ResourceKey>
	<Text>Action '{0}' is not defined</Text>
	<Comment />
	<Translation>
		<Text Culture="ja-JP">アクション '{0}' は定義されていません</Text>
		<Text Culture="zh-CHS">未定义动作'{0}'</Text>
		<Text Culture="ar">الإجراء "{0}" لم يتم تعريفه</Text>
		<Text Culture="es">La acción '{0}' no está definida nueva</Text>
		<Text Culture="pt">Ação '{0}' não está definida jj kk</Text>
		<Text Culture="it">L'azione '{0}' non è definita</Text>
	</Translation>
</Message>
```
The names of the elements are self-descriptive. Note that the ResourceFile element maintains the relative structure of where the RESX is for the solution.

### ResXToXml Task

So we need to generate this format automatically based on our resx files. 
```
<UsingTask AssemblyFile="SolutionLocalization.dll" TaskName="SolutionLocalization.ResXToXmlTask" />

<Target Name="GenerateXmlFromResx">
		<ResXToXmlTask DirectoryExclude="@(ExcludeDirectory)" Excludes="@(ExcludeExpression)" InputPath="$(RootDir)" OutputXml="$(OutputXls)" Culture="@(CultureInfo)" />
	</Target>

```



