# Stage 1: Build Environment
# Use the .NET SDK image. Using a specific version is good practice.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project and solution files first to leverage Docker layer caching.
# This step is only re-run if these files change.
COPY ["ProjectTaskMaster/TaskMaster.sln", "ProjectTaskMaster/"]
COPY ["ProjectTaskMaster/TaskMaster.Core/TaskMaster.Core.csproj", "ProjectTaskMaster/TaskMaster.Core/"]
COPY ["ProjectTaskMaster/TaskMaster.Infrastructure/TaskMaster.Infrastructure.csproj", "ProjectTaskMaster/TaskMaster.Infrastructure/"]
COPY ["ProjectTaskMaster/TaskMaster.API/TaskMaster.API.csproj", "ProjectTaskMaster/TaskMaster.API/"]

# Restore dependencies for the entire solution.
RUN dotnet restore "ProjectTaskMaster/TaskMaster.sln"

# Copy the rest of the source code.
COPY . .

# Publish the application directly from the source.
# This combines the build and publish steps into one, which is more efficient.
WORKDIR "/src/ProjectTaskMaster/TaskMaster.API"
RUN dotnet publish "TaskMaster.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Image
# Use the lightweight ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published output from the build stage.
COPY --from=build /app/publish .

# Set environment variables for running inside Kubernetes.
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose the port the application will listen on.
EXPOSE 80

# Define the user to run the application (Security Best Practice).
# The base aspnet image creates a low-privilege 'app' user for us.
USER app

# Set the entrypoint for the container.
ENTRYPOINT ["dotnet", "TaskMaster.API.dll"]