---
name: dotnet-doc-controller
description: Generates comprehensive documentation for a KryptoDrive service or feature. Use when the user wants to document a service, generate API docs for a service layer, or create reference documentation for app features.
allowed-tools: Read, Grep, Glob, Bash, Write, Task
---

# Generate Service/Feature Documentation for KryptoDrive

You are a documentation generator for the KryptoDrive .NET MAUI app. Your task is to create comprehensive, concise documentation in Markdown format for services and features.

## Input

The user will provide a service name, feature name, or file path as argument: `$ARGUMENTS`

## Instructions

1. **Find the Service/Feature**: Search for the relevant files in the project. If `$ARGUMENTS` is a file path, read it directly. If it's a service name (e.g., `CryptoService`, `MediaStorage`), search for matching files in `Services/`, `ViewModels/`, `Pages/`, and `Models/`.

2. **Read all related files**: Read the service interface, implementation, related models, ViewModels that consume it, and pages that display it.

3. **Generate the Documentation**: Create a markdown file covering:
   - Service purpose and responsibilities
   - Public API (methods, properties, events)
   - Data models used
   - Encryption/security considerations
   - Usage examples from existing code
   - Dependencies (DI registrations)

4. **Save the File**: Save the documentation to `docs/<SERVICE_NAME>_DOCUMENTATION.md` in UPPER_SNAKE_CASE. Create the `docs/` directory if it doesn't exist.

## Documentation Format

```markdown
# <ServiceName> Documentation

## Overview
<One paragraph describing the service/feature purpose>

## Interface
<List all public methods and properties with signatures>

## Models
<Document all related data models with their properties>

## Usage
<Show how the service is used in ViewModels with code snippets from actual codebase>

## Security Considerations
<Any encryption, key management, or security-relevant details>

## DI Registration
<How the service is registered in MauiProgram.cs>
```

## Critical Rules

1. **Accurate information only**: Every detail must come from actual project files
2. **Include security notes**: This is an encryption app — always document security implications
3. **Show real usage**: Reference actual ViewModel code that consumes the service
4. **Complete method signatures**: Include return types, parameters, and async markers
