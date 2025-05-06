#!/bin/bash

dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet XR50TrainingAssetRepo.dll
