.NET 8, C#, ASP.NET, HtmlAgilityPack, Postgre EF Core, ClickHouse driver, RabbitMQ, docker-compose. 

(Не готово)

Идея по парсингу была такая (парсеры в целом написаны, но не отлажены, проверял запросы постманом):

По запросу пользователя или по решению сервиса-планировщика создание задачи на сбор данных\
->\
RabbitMQ queue CollectProductInfosTasks (Year, FromMonth, ToMonth)\
->\
ParseProductReleasesHostedService (проект ParseProductPipeline) (в качестве микросервиса или внутри основного приложения) (обращение к API стима)\
->\
RabbitMQ queue CollectedProductReleases (AppId, Title, Year, Month, DateOfMonth?, FetchedAt)\
->\
ParseAppDataHostedService  (проект ParseProductPipeline) (в качестве микросервиса или внутри основного приложения) (обращение к API стима)\
->\
RabbitMQ queue CollectedProductReleasesAndData (AppId, Title, Year, Month, DateOfMonth?, FetchedAt) + (ShortDescription, Genres, ...)\
->\
ParseFollowersHostedService (проект ParseProductPipeline) (в качестве микросервиса или внутри основного приложения) (обращение к API стима)\
->\
RabbitMQ queue CollectedProductReleasesAndDataAndFollowersCount (...) + (...) + (FollowersCount)\
->\
Микросервисом или HostedService'ом изнутри основного приложения асинхронно отправлять это в таблицу в ClickHouse.

Для ClickHouse миграции сделал, для Postgres надо кнопку в IDE нажать чтоб сгенерировались.\
В Postgres пока что планируется хранить миграции ClickHouse и историю задач на обновление информации по играм.

В WebApi "разметил" методы, в целом ничего не делает, по идее должен отправлять запросы к ClickHouse.\
Можно добавить метод генерации JWT-токена и проверять его через middleware или через авторизацию.
