CREATE DATABASE IF NOT EXISTS companie CHARACTER SET utf8 COLLATE utf8_romanian_ci;
USE companie;

DROP TABLE IF EXISTS Angajati;
DROP TABLE IF EXISTS Departamente;

CREATE TABLE Departamente (
    DepartamentID INT AUTO_INCREMENT PRIMARY KEY,
    DenumireDepartament VARCHAR(100) NOT NULL
);

CREATE TABLE Angajati (
    AngajatID INT AUTO_INCREMENT PRIMARY KEY,
    DepartamentID INT NOT NULL,
    Nume VARCHAR(100) NOT NULL,
    Prenume VARCHAR(100) NOT NULL,
    Functie VARCHAR(100) NOT NULL,
    Salariu DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (DepartamentID) REFERENCES Departamente(DepartamentID) ON DELETE CASCADE
);

CREATE OR REPLACE VIEW vw_AngajatiDepartamente AS
SELECT
    a.AngajatID,
    a.Nume,
    a.Prenume,
    a.Functie,
    a.Salariu,
    d.DenumireDepartament
FROM Angajati a
INNER JOIN Departamente d ON a.DepartamentID = d.DepartamentID;

DROP PROCEDURE IF EXISTS sp_AngajatiDupaDepartament;
DELIMITER $$
CREATE PROCEDURE sp_AngajatiDupaDepartament(IN p_DepartamentID INT)
BEGIN
    SELECT
        a.AngajatID,
        a.Nume,
        a.Prenume,
        a.Functie,
        a.Salariu,
        d.DenumireDepartament
    FROM Angajati a
    INNER JOIN Departamente d ON a.DepartamentID = d.DepartamentID
    WHERE a.DepartamentID = p_DepartamentID;
END$$
DELIMITER ;

INSERT INTO Departamente (DenumireDepartament) VALUES
('IT'),
('Resurse Umane'),
('Financiar'),
('Marketing');

INSERT INTO Angajati (DepartamentID, Nume, Prenume, Functie, Salariu) VALUES
(1, 'Ionescu', 'Ion', 'Programator', 5500.00),
(1, 'Popa', 'Andrei', 'Analist', 4800.00),
(2, 'Dumitru', 'Maria', 'HR Manager', 4200.00),
(3, 'Constantin', 'Elena', 'Contabil', 3900.00),
(4, 'Gheorghe', 'Mihai', 'Marketing Specialist', 4100.00);
