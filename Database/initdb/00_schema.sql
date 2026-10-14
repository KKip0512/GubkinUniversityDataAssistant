-- Схема БД университета. Выполняется один раз при первом старте контейнера db
-- (до загрузки данных из Database/*_rows.sql). Порядок создания учитывает внешние ключи.
-- Данные генерирует Database/generator/generate_data.py. Снимок: 2026/2027 учебный год, осенний семестр.
-- Описания таблиц и колонок задаются через COMMENT ON: бэкенд читает их из БД
-- и передаёт модели вместе со структурой, поэтому они должны быть понятны без этого файла.

CREATE TABLE public.faculties (
    faculty_id  integer PRIMARY KEY,
    name        varchar NOT NULL,
    code        varchar NOT NULL
);

COMMENT ON TABLE public.faculties IS 'Факультеты университета';
COMMENT ON COLUMN public.faculties.name IS 'Полное название факультета';
COMMENT ON COLUMN public.faculties.code IS 'Краткое обозначение факультета';

CREATE TABLE public.education_levels (
    level_id  integer PRIMARY KEY,
    name      varchar NOT NULL UNIQUE
);

COMMENT ON TABLE public.education_levels IS 'Уровни высшего образования';
COMMENT ON COLUMN public.education_levels.name IS 'Название уровня образования';

CREATE TABLE public.programs (
    program_id      integer PRIMARY KEY,
    faculty_id      integer NOT NULL REFERENCES public.faculties (faculty_id),
    level_id        integer NOT NULL REFERENCES public.education_levels (level_id),
    code            varchar NOT NULL,
    title           varchar NOT NULL,
    duration_years  integer NOT NULL
);

COMMENT ON TABLE public.programs IS 'Образовательные программы (направления подготовки) бакалавриата, магистратуры и аспирантуры';
COMMENT ON COLUMN public.programs.level_id IS 'Уровень образования программы';
COMMENT ON COLUMN public.programs.code IS 'Код направления подготовки (например 09.03.01), у аспирантуры — шифр научной специальности (например 2.8.4)';
COMMENT ON COLUMN public.programs.title IS 'Название программы';
COMMENT ON COLUMN public.programs.duration_years IS 'Нормативный срок обучения в годах';

CREATE TABLE public.subjects (
    subject_id  integer PRIMARY KEY,
    title       varchar NOT NULL UNIQUE
);

COMMENT ON TABLE public.subjects IS 'Учебные дисциплины (предметы)';
COMMENT ON COLUMN public.subjects.title IS 'Название дисциплины';

CREATE TABLE public.curriculum (
    curriculum_id  integer PRIMARY KEY,
    program_id     integer NOT NULL REFERENCES public.programs (program_id),
    subject_id     integer NOT NULL REFERENCES public.subjects (subject_id),
    semester       integer NOT NULL CHECK (semester BETWEEN 1 AND 8),
    control_form   varchar NOT NULL CHECK (control_form IN ('Экзамен', 'Зачёт')),
    lecture_hours  integer NOT NULL,
    seminar_hours  integer NOT NULL,
    UNIQUE (program_id, subject_id, semester)
);

COMMENT ON TABLE public.curriculum IS 'Учебный план: какие дисциплины изучаются на программе в каком семестре';
COMMENT ON COLUMN public.curriculum.semester IS 'Номер семестра от начала обучения (1–8): нечётный — осенний, чётный — весенний';
COMMENT ON COLUMN public.curriculum.control_form IS 'Форма итогового контроля по дисциплине в семестре';
COMMENT ON COLUMN public.curriculum.lecture_hours IS 'Часы лекций за семестр; 0 — у дисциплины нет лекций';
COMMENT ON COLUMN public.curriculum.seminar_hours IS 'Часы семинаров (практических занятий) за семестр';

CREATE TABLE public.streams (
    stream_id       integer PRIMARY KEY,
    name            varchar NOT NULL UNIQUE,
    program_id      integer NOT NULL REFERENCES public.programs (program_id),
    admission_year  integer NOT NULL,
    course          integer NOT NULL CHECK (course BETWEEN 1 AND 4),
    study_form      varchar NOT NULL CHECK (study_form IN ('Очная', 'Заочная'))
);

COMMENT ON TABLE public.streams IS 'Потоки: группы одной программы, года набора и формы обучения, которые вместе слушают лекции';
COMMENT ON COLUMN public.streams.name IS 'Название потока, например АС-24 или АС-24, поток 2';
COMMENT ON COLUMN public.streams.admission_year IS 'Год набора (поступления)';
COMMENT ON COLUMN public.streams.course IS 'Курс (год обучения, 1–4) в 2026/2027 учебном году';
COMMENT ON COLUMN public.streams.study_form IS 'Форма обучения';

CREATE TABLE public.student_groups (
    group_id        integer PRIMARY KEY,
    name            varchar NOT NULL UNIQUE,
    program_id      integer NOT NULL REFERENCES public.programs (program_id),
    stream_id       integer NOT NULL REFERENCES public.streams (stream_id),
    admission_year  integer NOT NULL,
    course          integer NOT NULL CHECK (course BETWEEN 1 AND 4),
    study_form      varchar NOT NULL CHECK (study_form IN ('Очная', 'Заочная'))
);

COMMENT ON TABLE public.student_groups IS 'Академические группы студентов; семинары ведутся по группам';
COMMENT ON COLUMN public.student_groups.name IS 'Название группы, например АС-24-01 (24 — год набора), у заочных групп — с буквой з: ЭКз-24-01';
COMMENT ON COLUMN public.student_groups.stream_id IS 'Поток, в который входит группа';
COMMENT ON COLUMN public.student_groups.admission_year IS 'Год набора (поступления)';
COMMENT ON COLUMN public.student_groups.course IS 'Курс (год обучения, 1–4) в 2026/2027 учебном году';
COMMENT ON COLUMN public.student_groups.study_form IS 'Форма обучения';

CREATE TABLE public.employees (
    employee_id      integer PRIMARY KEY,
    last_name        varchar NOT NULL,
    first_name       varchar NOT NULL,
    middle_name      varchar,
    gender           varchar NOT NULL CHECK (gender IN ('М', 'Ж')),
    passport_series  varchar,
    passport_number  varchar,
    email            varchar,
    phone_number     varchar,
    address          varchar,
    birth_date       date,
    faculty_id       integer NOT NULL REFERENCES public.faculties (faculty_id),
    role             varchar NOT NULL,
    academic_degree  varchar,
    hire_year        integer,
    employment_form  varchar
);

COMMENT ON TABLE public.employees IS 'Преподаватели факультетов';
COMMENT ON COLUMN public.employees.last_name IS 'Фамилия';
COMMENT ON COLUMN public.employees.first_name IS 'Имя';
COMMENT ON COLUMN public.employees.middle_name IS 'Отчество';
COMMENT ON COLUMN public.employees.gender IS 'Пол';
COMMENT ON COLUMN public.employees.birth_date IS 'Дата рождения';
COMMENT ON COLUMN public.employees.faculty_id IS 'Факультет, на котором работает преподаватель';
COMMENT ON COLUMN public.employees.role IS 'Должность';
COMMENT ON COLUMN public.employees.academic_degree IS 'Учёная степень; NULL — без степени';
COMMENT ON COLUMN public.employees.hire_year IS 'Год приёма на работу';
COMMENT ON COLUMN public.employees.employment_form IS 'Форма занятости';

CREATE TABLE public.students (
    student_id           integer PRIMARY KEY,
    last_name            varchar NOT NULL,
    first_name           varchar NOT NULL,
    middle_name          varchar,
    gender               varchar NOT NULL CHECK (gender IN ('М', 'Ж')),
    passport_series      varchar,
    passport_number      varchar,
    email                varchar,
    phone_number         varchar,
    address              varchar,
    birth_date           date,
    program_id           integer NOT NULL REFERENCES public.programs (program_id),
    group_id             integer NOT NULL REFERENCES public.student_groups (group_id),
    admission_year       integer NOT NULL,
    course               integer NOT NULL CHECK (course BETWEEN 1 AND 4),
    study_form           varchar NOT NULL CHECK (study_form IN ('Очная', 'Заочная')),
    is_budget            boolean NOT NULL,
    military_training    boolean NOT NULL DEFAULT false,
    military_start_year  integer,
    military_end_year    integer,
    CHECK (NOT military_training OR gender = 'М'),
    CHECK (military_training = (military_start_year IS NOT NULL)),
    CHECK (military_end_year = military_start_year + 2)
);

COMMENT ON TABLE public.students IS 'Студенты бакалавриата, магистратуры и аспирантуры';
COMMENT ON COLUMN public.students.last_name IS 'Фамилия';
COMMENT ON COLUMN public.students.first_name IS 'Имя';
COMMENT ON COLUMN public.students.middle_name IS 'Отчество';
COMMENT ON COLUMN public.students.gender IS 'Пол';
COMMENT ON COLUMN public.students.birth_date IS 'Дата рождения';
COMMENT ON COLUMN public.students.program_id IS 'Программа, на которой учится студент';
COMMENT ON COLUMN public.students.group_id IS 'Академическая группа студента';
COMMENT ON COLUMN public.students.admission_year IS 'Год поступления';
COMMENT ON COLUMN public.students.course IS 'Курс (год обучения, 1–4) в 2026/2027 учебном году';
COMMENT ON COLUMN public.students.study_form IS 'Форма обучения';
COMMENT ON COLUMN public.students.is_budget IS 'true — обучение на бюджетной основе, false — на платной (контракт)';
COMMENT ON COLUMN public.students.military_training IS 'Обучается на военной кафедре (только юноши очной формы бакалавриата, со 2 курса, 2 года)';
COMMENT ON COLUMN public.students.military_start_year IS 'Год начала обучения на военной кафедре; NULL — не обучается';
COMMENT ON COLUMN public.students.military_end_year IS 'Год окончания обучения на военной кафедре';

CREATE TABLE public.ege_scores (
    score_id      integer PRIMARY KEY,
    student_id    integer NOT NULL REFERENCES public.students (student_id),
    subject_name  varchar NOT NULL,
    score         integer NOT NULL CHECK (score BETWEEN 0 AND 100),
    exam_year     integer NOT NULL
);

COMMENT ON TABLE public.ege_scores IS 'Баллы ЕГЭ студентов (есть только у студентов бакалавриата)';
COMMENT ON COLUMN public.ege_scores.subject_name IS 'Предмет ЕГЭ';
COMMENT ON COLUMN public.ege_scores.score IS 'Балл ЕГЭ, 0–100';
COMMENT ON COLUMN public.ege_scores.exam_year IS 'Год сдачи ЕГЭ';

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

COMMENT ON TABLE public.applications IS 'Заявления о поступлении на бакалавриат: у каждого студента бакалавриата одно заявление со статусом Зачислен (на его программу) и, возможно, заявления на другие программы';
COMMENT ON COLUMN public.applications.student_id IS 'Студент, подавший заявление';
COMMENT ON COLUMN public.applications.program_id IS 'Программа, на которую подано заявление';
COMMENT ON COLUMN public.applications.submission_date IS 'Дата подачи заявления';
COMMENT ON COLUMN public.applications.is_budget IS 'true — заявление на бюджетное место, false — на платное';
COMMENT ON COLUMN public.applications.has_original_document IS 'Подан ли оригинал документа об образовании';
COMMENT ON COLUMN public.applications.total_score IS 'Конкурсный балл: сумма трёх ЕГЭ и баллов за индивидуальные достижения';
COMMENT ON COLUMN public.applications.status IS 'Статус заявления';

CREATE TABLE public.grades (
    grade_id       integer PRIMARY KEY,
    student_id     integer NOT NULL REFERENCES public.students (student_id),
    subject_id     integer NOT NULL REFERENCES public.subjects (subject_id),
    semester       integer NOT NULL CHECK (semester BETWEEN 1 AND 8),
    academic_year  varchar NOT NULL,
    brs_score      integer NOT NULL CHECK (brs_score BETWEEN 0 AND 100),
    grade          varchar,
    exam_date      date
);

COMMENT ON TABLE public.grades IS 'Баллы БРС студентов по дисциплинам: за завершённые семестры — итоговые с оценкой, за текущий осенний семестр 2026/2027 — текущий рейтинг без оценки';
COMMENT ON COLUMN public.grades.semester IS 'Номер семестра от начала обучения (1–8): нечётный — осенний, чётный — весенний';
COMMENT ON COLUMN public.grades.academic_year IS 'Учебный год';
COMMENT ON COLUMN public.grades.brs_score IS 'Балл по балльно-рейтинговой системе (БРС), 0–100; при grade IS NULL — текущий рейтинг в идущем семестре, иначе итоговый балл';
COMMENT ON COLUMN public.grades.grade IS 'Итоговая оценка за экзамен или зачёт; NULL — текущий семестр, итоговой оценки ещё нет';
COMMENT ON COLUMN public.grades.exam_date IS 'Дата экзамена или зачёта; NULL — семестр ещё идёт';

CREATE TABLE public.lectures (
    lecture_id     integer PRIMARY KEY,
    employee_id    integer NOT NULL REFERENCES public.employees (employee_id),
    subject_id     integer NOT NULL REFERENCES public.subjects (subject_id),
    stream_id      integer NOT NULL REFERENCES public.streams (stream_id),
    academic_year  varchar NOT NULL,
    semester       integer NOT NULL,
    hours          integer NOT NULL
);

COMMENT ON TABLE public.lectures IS 'Лекции: какой преподаватель какую дисциплину читает какому потоку';
COMMENT ON COLUMN public.lectures.employee_id IS 'Лектор';
COMMENT ON COLUMN public.lectures.academic_year IS 'Учебный год';
COMMENT ON COLUMN public.lectures.semester IS 'Номер семестра от начала обучения потока: нечётный — осенний, чётный — весенний';
COMMENT ON COLUMN public.lectures.hours IS 'Часы лекций за семестр';

CREATE TABLE public.seminars (
    seminar_id     integer PRIMARY KEY,
    employee_id    integer NOT NULL REFERENCES public.employees (employee_id),
    subject_id     integer NOT NULL REFERENCES public.subjects (subject_id),
    group_id       integer NOT NULL REFERENCES public.student_groups (group_id),
    academic_year  varchar NOT NULL,
    semester       integer NOT NULL,
    hours          integer NOT NULL
);

COMMENT ON TABLE public.seminars IS 'Семинары (практические занятия): какой преподаватель по какой дисциплине ведёт занятия в какой группе';
COMMENT ON COLUMN public.seminars.employee_id IS 'Преподаватель, ведущий семинары';
COMMENT ON COLUMN public.seminars.academic_year IS 'Учебный год';
COMMENT ON COLUMN public.seminars.semester IS 'Номер семестра от начала обучения группы: нечётный — осенний, чётный — весенний';
COMMENT ON COLUMN public.seminars.hours IS 'Часы семинаров за семестр';

-- Индексы под типичные JOIN'ы, которые генерирует модель
CREATE INDEX ON public.programs       (faculty_id);
CREATE INDEX ON public.programs       (level_id);
CREATE INDEX ON public.curriculum     (program_id);
CREATE INDEX ON public.curriculum     (subject_id);
CREATE INDEX ON public.streams        (program_id);
CREATE INDEX ON public.student_groups (program_id);
CREATE INDEX ON public.student_groups (stream_id);
CREATE INDEX ON public.employees      (faculty_id);
CREATE INDEX ON public.students       (program_id);
CREATE INDEX ON public.students       (group_id);
CREATE INDEX ON public.ege_scores     (student_id);
CREATE INDEX ON public.applications   (student_id);
CREATE INDEX ON public.applications   (program_id);
CREATE INDEX ON public.grades         (student_id);
CREATE INDEX ON public.grades         (subject_id);
CREATE INDEX ON public.lectures       (employee_id);
CREATE INDEX ON public.lectures       (subject_id);
CREATE INDEX ON public.lectures       (stream_id);
CREATE INDEX ON public.seminars       (employee_id);
CREATE INDEX ON public.seminars       (subject_id);
CREATE INDEX ON public.seminars       (group_id);
