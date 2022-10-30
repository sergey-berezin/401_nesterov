# Практические задания на .NET

## Установка компонента для асинхронного получения эмбеддингов лиц

Клонирование репозитория
```
git clone https://github.com/dmitrylala/401_nesterov.git && cd 401_nesterov/
```
Директория куда будет установлен NuGet-пакет с компонентом
```
mkdir packages
```
Добавление директории в список источников NuGet-пакетов (указывается абсолютный путь до директории, либо через ~)
```
dotnet nuget add source ~/Projects/401_nesterov/packages/ --name LabPackages
```

Также необходимо скачать веса модели и поместить в директорию с компонентом /401_nesterov/FaceEmbeddingsAsync/.
https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx

Упаковка компонента в NuGet-пакет (версия 1.0.7)
```
dotnet pack FaceEmbeddingsAsync/FaceEmbeddingsAsync.csproj --output packages
```

## Консольное приложение
Добавление NuGet-пакета и запуск
```
cd ConsoleApp && dotnet add package FaceEmbeddingsAsync --version 1.0.7 && dotnet run
```

## Оконное приложение
Добавление NuGet-пакета и запуск
```
cd WindowApp && dotnet add package FaceEmbeddingsAsync --version 1.0.7 && dotnet run
```
