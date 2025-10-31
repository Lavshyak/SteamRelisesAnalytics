CREATE TABLE IF NOT EXISTS games
(
    app_id            UInt64,
    title             String,
    release_year      UInt16,
    release_month     UInt8, -- 1..12
    release_day       UInt8, -- 1..примерно 30, 0 - не указано
    genres            Array(String),
    categories        Array(String),
    followers         UInt64,
    store_url         String,
    image_url         String,
    short_description String,
    platforms         Array(String),
    fetched_at        DateTime DEFAULT now()
    )
    ENGINE = MergeTree
    ORDER BY (release_year, release_month, release_day, fetched_at);

ALTER TABLE games ADD INDEX idx_genres genres TYPE set(0) GRANULARITY 1;
ALTER TABLE games ADD INDEX idx_categories categories TYPE set(0) GRANULARITY 1;
ALTER TABLE games ADD INDEX idx_platforms platforms TYPE set(0) GRANULARITY 1;
ALTER TABLE games ADD INDEX idx_release (release_year, release_month, release_day) TYPE minmax GRANULARITY 1;