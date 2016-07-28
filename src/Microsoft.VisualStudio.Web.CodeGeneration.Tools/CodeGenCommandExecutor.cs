// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    public class CodeGenCommandExecutor
    {
        private ProjectContext _projectContext;
        private string[] _codeGenArguments;
        private string _configuration;
        private string _nugetPackageDir;
        private ILogger _logger;

        public CodeGenCommandExecutor(ProjectContext projectContext, string[] codeGenArguments, string configuration, string nugetPackageDir, ILogger logger)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }
            if (codeGenArguments == null)
            {
                throw new ArgumentNullException(nameof(codeGenArguments));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _projectContext = projectContext;
            _codeGenArguments = codeGenArguments;
            _configuration = configuration;
            _logger = logger;
            _nugetPackageDir = nugetPackageDir;
        }

        public int Execute()
        {
            var serviceProvider = new ServiceProvider();
            AddFrameworkServices(serviceProvider, _projectContext, _nugetPackageDir);
            AddCodeGenerationServices(serviceProvider);
            var codeGenCommand = serviceProvider.GetService<CodeGenCommand>();
            codeGenCommand.Execute(_codeGenArguments);
            return 0;
        }

        private void AddFrameworkServices(ServiceProvider serviceProvider, ProjectContext context, string nugetPackageDir)
        {
            var applicationInfo = new ApplicationInfo(context.RootProject.Identity.Name, context.ProjectDirectory);
            serviceProvider.Add<ProjectContext>(context);
            serviceProvider.Add<CodeAnalysis.Workspace>(context.CreateRoslynWorkspace());
            serviceProvider.Add<IApplicationInfo>(applicationInfo);
            serviceProvider.Add<ICodeGenAssemblyLoadContext>(new DefaultAssemblyLoadContext());
            serviceProvider.Add<ILibraryManager>(new LibraryManager(context));
            serviceProvider.Add<ILibraryExporter>(new LibraryExporter(context, applicationInfo));
        }

        private void AddCodeGenerationServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            //Ordering of services is important here
            serviceProvider.Add(typeof(ILogger), _logger);
            serviceProvider.Add(typeof(IFilesLocator), new FilesLocator());

            serviceProvider.AddServiceWithDependencies<ICodeGeneratorAssemblyProvider, DefaultCodeGeneratorAssemblyProvider>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorLocator, CodeGeneratorsLocator>();
            serviceProvider.AddServiceWithDependencies<CodeGenCommand, CodeGenCommand>();

            serviceProvider.AddServiceWithDependencies<ICompilationService, RoslynCompilationService>();
            serviceProvider.AddServiceWithDependencies<ITemplating, RazorTemplating>();

            serviceProvider.AddServiceWithDependencies<IPackageInstaller, PackageInstaller>();

            serviceProvider.AddServiceWithDependencies<IModelTypesLocator, ModelTypesLocator>();
            serviceProvider.AddServiceWithDependencies<ICodeGeneratorActionsService, CodeGeneratorActionsService>();

            serviceProvider.AddServiceWithDependencies<IDbContextEditorServices, DbContextEditorServices>();
            serviceProvider.AddServiceWithDependencies<IEntityFrameworkService, EntityFrameworkServices>();
        }
    }
}