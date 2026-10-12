-- Схема БД университета. Выполняется один раз при первом старте контейнера db
-- (до загрузки данных из Database/*_rows.sql). Порядок создания учитывает внешние ключи.

CREATE TABLE public.faculties (
    faculty_id  integer PRIMARY KEY,
    name        varchar NOT NULL,
    code        varchar NOT NULL
);

CREATE TABLE public.employees (
    employee_id      integer PRIMARY KEY,
    last_name        varchar NOT NULL,
    first_name       varchar NOT NULL,
    middle_name      varchar,
    passport_series  varchar,
    passport_number  varchar,
    email            varchar,
    phone_number     varchar,
    address          varchar,
    birth_date       date,
    faculty_id       integer REFERENCES public.faculties (faculty_id),
    hire_year        integer,
    employment_form  varchar,
    role             varchar
);

CREATE TABLE public.programs (
    program_id      integer PRIMARY KEY,
    faculty_id      integer NOT NULL REFERENCES public.faculties (faculty_id),
    title           varchar NOT NULL,
    degree_type     varchar,
    duration_years  integer
);

CREATE TABLE public.courses (
    course_id    integer PRIMARY KEY,
    title        varchar NOT NULL,
    faculty_id   integer REFERENCES public.faculties (faculty_id),
    employee_id  integer REFERENCES public.employees (employee_id)
);

CREATE TABLE public.students (
    student_id       integer PRIMARY KEY,
    last_name        varchar NOT NULL,
    first_name       varchar NOT NULL,
    middle_name      varchar,
    passport_series  varchar,
    passport_number  varchar,
    email            varchar,
    phone_number     varchar,
    address          varchar,
    birth_date       date,
    program_id       integer REFERENCES public.programs (program_id),
    admission_year   integer,
    study_form       varchar
);

CREATE TABLE public.applications (
    application_id         integer PRIMARY KEY,
    student_id             integer NOT NULL REFERENCES public.students (student_id),
    program_id             integer NOT NULL REFERENCES public.programs (program_id),
    submission_date        date,
    is_budget              boolean,
    has_original_document  boolean,
    total_score            integer,
    status                 varchar
);

CREATE TABLE public.exam_scores (
    score_id      integer PRIMARY KEY,
    student_id    integer NOT NULL REFERENCES public.students (student_id),
    subject_name  varchar NOT NULL,
    score         integer
);

CREATE TABLE public.grades (
    grade_id    integer PRIMARY KEY,
    student_id  integer NOT NULL REFERENCES public.students (student_id),
    course_id   integer NOT NULL REFERENCES public.courses (course_id),
    brs_score   integer,
    exam_date   date
);

-- Индексы под типичные JOIN'ы, которые генерирует модель
CREATE INDEX ON public.students     (program_id);
CREATE INDEX ON public.applications (student_id);
CREATE INDEX ON public.applications (program_id);
CREATE INDEX ON public.exam_scores  (student_id);
CREATE INDEX ON public.grades       (student_id);
CREATE INDEX ON public.grades       (course_id);
CREATE INDEX ON public.courses      (employee_id);
