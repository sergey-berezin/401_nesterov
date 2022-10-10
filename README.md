# Практические задания на .NET

## Установка компонента для асинхронного получения эмбеддингов лиц
```
# клонирование репозитория и чекаут на ветку с компонентом
git clone https://github.com/dmitrylala/401_nesterov.git && cd 401_nesterov/
git checkout onnx_component

# директория куда будет установлен NuGet-пакет с компонентом
mkdir packages

# добавление директории в список источников NuGet-пакетов (указывается абсолютный путь до директории, либо через ~)
dotnet nuget add source ~/Projects/401_nesterov/packages/ --name LabPackages

# необходимо скачать веса модели и поместить в директорию с компонентом /401_nesterov/FaceEmbeddingsAsync/
# https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx

# упаковка компонента в NuGet-пакет (версия 1.0.7)
dotnet pack FaceEmbeddingsAsync/FaceEmbeddingsAsync.csproj --output packages
```

## Добавление пакета в консольное приложение и запуск
```
cd ConsoleApp && dotnet add package FaceEmbeddingsAsync --version 1.0.7 && dotnet run
```
