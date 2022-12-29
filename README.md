# Практические задания на .NET

## Компонент для асинхронного получения эмбеддингов лиц

Пакет использует готовую [нейросеть](https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx) в .onnx формате для получения эмбеддингов лиц, предоставляет асинхронный потокобезопасный API для инференса.

Пакет опубликован на [NuGet](https://www.nuget.org/packages/FaceEmbeddingsAsync/#versions-body-tab), можно добавить его в проект через .NET CLI:
```
dotnet add package FaceEmbeddingsAsync --version 1.0.7
```


## Консольное приложение
Консольное приложение иллюстрирует работу с компонентом для асинхронного получения эмбеддингов.
```
cd ConsoleApp && dotnet run
```

## Сервер и клиент
Сервер выполняет работу по обработке изображений и хранению эмбеддингов в базе данных.
```
cd Server && dotnet build && dotnet run
```

Клиент позволяет пользователю выбирать изображения для анализа и рассчитывать попарные расстояния и косинусные меры близости между ними на основе эмбеддингов.
```
cd WindowApp && dotnet build && dotnet run
```

Также реализован клиент в веб-браузере на python3 с помощью библиотеки streamlit (нужен poetry):
```
cd WebApp && poetry install && poetry run startup
```
