-- 1. Создание базы данных
CREATE DATABASE IF NOT EXISTS TrainingCenter;
USE TrainingCenter;

-- 2. Таблица студентов (Students)
CREATE TABLE Student (
    StudentId INT PRIMARY KEY AUTO_INCREMENT,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100) UNIQUE NOT NULL
);

-- 3. Таблица курсов (Courses)
CREATE TABLE Course (
    CourseId INT PRIMARY KEY AUTO_INCREMENT,
    Title VARCHAR(100) NOT NULL,
    Trainer VARCHAR(100) NOT NULL,
    Price DECIMAL(10, 2) NOT NULL CHECK (Price > 0),
    DurationDays INT NOT NULL
);

-- 4. Таблица регистраций (Enrollments)
CREATE TABLE Enrollment (
    EnrollmentId INT PRIMARY KEY AUTO_INCREMENT,
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    EnrollmentDate DATE NOT NULL,
    PaymentStatus VARCHAR(20) DEFAULT 'Unpaid',
    -- Внешние ключи
    FOREIGN KEY (StudentId) REFERENCES Student(StudentId),
    FOREIGN KEY (CourseId) REFERENCES Course(CourseId),
    -- Ограничение: студент не может записаться на один и тот же курс дважды
    UNIQUE(StudentId, CourseId)
);

-- ==========================================
-- ЗАПОЛНЕНИЕ ТЕСТОВЫМИ ДАННЫМИ (REQUIREMENT 8)
-- ==========================================

-- Добавляем 6 студентов
INSERT INTO Student (FirstName, LastName, Phone, Email) VALUES 
('John', 'Smith', '123456789', 'john.smith@example.com'),
('Maria', 'Garcia', '987654321', 'maria.g@example.com'),
('Ion', 'Ceban', '060112233', 'ion.ceban@mail.md'),
('Elena', 'Popescu', '079445566', 'elena.p@mail.md'),
('Alex', 'Wong', '555667788', 'alex.w@example.com'),
('Anna', 'Muller', '444332211', 'anna.m@example.com');

-- Добавляем 6 курсов
INSERT INTO Course (Title, Trainer, Price, DurationDays) VALUES 
('C# Programming', 'Victor Procopiuc', 1500.00, 30),
('Web Development', 'Andrei Lupu', 1800.00, 45),
('SQL Database Expert', 'Marina Balan', 1200.00, 20),
('Project Management', 'George Iliescu', 2500.00, 15),
('Python for Beginners', 'Sergiu Nicu', 1400.00, 25),
('UI/UX Design', 'Dina Rotaru', 2000.00, 40);

-- Добавляем 8 регистраций (некоторые студенты на нескольких курсах)
INSERT INTO Enrollment (StudentId, CourseId, EnrollmentDate, PaymentStatus) VALUES 
(1, 1, '2023-10-01', 'Paid'),     -- John на C#
(1, 3, '2023-10-05', 'Unpaid'),   -- John на SQL
(2, 1, '2023-10-02', 'Paid'),     -- Maria на C#
(3, 2, '2023-10-10', 'Paid'),     -- Ion на Web
(4, 4, '2023-10-12', 'Paid'),     -- Elena на PM
(5, 5, '2023-10-15', 'Unpaid'),   -- Alex на Python
(6, 6, '2023-10-20', 'Paid'),     -- Anna на Design
(2, 5, '2023-10-22', 'Paid');     -- Maria на Python
