using System.Runtime.InteropServices;

namespace CSConsoleApp
{
    public static class Program
    {
        public static void Main()
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            var filePath = System.IO.Directory.GetFiles(currentDirectory, "*.csv").First();

            IReadOnlyList<MovieCredit> movieCredits = null;
            try
            {
                var parser = new MovieCreditsParser(filePath);
                movieCredits = parser.Parse();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Не удалось распарсить csv");
                Environment.Exit(1);
            }

            // 1. Фильмы Спилберга
            var spielbergMovies = movieCredits
                .Where(mc => mc.Crew.Any(c => c.Name == "Steven Spielberg" && c.Job == "Director"))
                .Select(mc => mc.Title);
            PrintResults("1. Фильмы Спилберга", spielbergMovies);

            // 2. Персонажи Тома Хэнкса
            var hanksCharacters = movieCredits
                .SelectMany(mc => mc.Cast.Where(c => c.Name == "Tom Hanks")
                .Select(c => new { Movie = mc.Title }));
            PrintResults("2. Персонажи Тома Хэнкса", hanksCharacters.Select(h => $"{h.Movie}: {h.Character}"));

            // 3. Топ-5 фильмов по количеству актеров
            var top5MoviesByActors = movieCredits
                .OrderByDescending(mc => mc.Cast.Count)
                .Take(5)
                .Select(mc => $"{mc.Title}: {mc.Cast.Count} actors");
            PrintResults("3. Топ-5 фильмов по количеству актеров", top5MoviesByActors);

            // 4. Топ-10 самых востребованных актеров
            var top10Actors = movieCredits
                .SelectMany(mc => mc.Cast)
                .GroupBy(c => c.Name)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key}: {g.Count()} фильмов");
            PrintResults("4. Топ-10 актеров", top10Actors);

            // 5. Уникальные департаменты
            var departments = movieCredits
                .SelectMany(mc => mc.Crew)
                .Select(c => c.Department)
                .Distinct();
            PrintResults("5. Уникальные департаменты", departments);

            // 6. Фильмы с Хансом Зиммером
            var zimmerMovies = movieCredits
                .Where(mc => mc.Crew.Any(c => c.Name == "Hans Zimmer" && c.Job == "Original Music Composer"))
                .Select(mc => mc.Title);
            PrintResults("6. Фильмы с музыкой Ханса Циммера", zimmerMovies);

            // 7. Словарь ID фильма -> режиссер
            var directorDict = movieCredits
                .ToDictionary(
                    mc => mc.MovieId,
                    mc => mc.Crew.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown"
                );

            // 8. Фильмы с Брэдом Питтом и Джорджем Клуни
            var pittClooneyMovies = movieCredits
                .Where(mc => mc.Cast.Any(c => c.Name == "Brad Pitt") && 
                            mc.Cast.Any(c => c.Name == "George Clooney"))
                .Select(mc => mc.Title);
            PrintResults("8. Фильмы с Питтом и Клуни", pittClooneyMovies);

            // 9. Количество работников Camera department
            var cameraCrewCount = movieCredits
                .SelectMany(mc => mc.Crew)
                .Count(c => c.Department == "Camera");
            PrintResults("9. Работников в Camera department", cameraCrewCount);

            // 10. Люди в Титанике (Cast + Crew)
            var titanicPeople = movieCredits
                .Where(mc => mc.Title == "Titanic")
                .SelectMany(mc => mc.Cast.Select(c => c.Name)
                    .Intersect(mc.Crew.Select(c => c.Name)));
            PrintResults("10. Люди и в Cast, и в Crew Титаника", titanicPeople);

            // 11. "Внутренний круг" Тарантино
            var tarantinoInnerCircle = movieCredits
                .Where(mc => mc.Crew.Any(c => c.Name == "Quentin Tarantino" && c.Job == "Director"))
                .SelectMany(mc => mc.Crew)
                .Where(c => c.Name != "Quentin Tarantino")
                .GroupBy(c => new { c.Name, c.Department })
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key.Name} ({g.Key.Department}): {g.Count()} films");
            PrintResults("11. Внутренний круг Тарантино", tarantinoInnerCircle);

            // 12. Экранные дуэты
            var actorPairs = movieCredits
                .SelectMany(mc => mc.Cast
                    .SelectMany(c1 => mc.Cast
                        .Where(c2 => c1.Name.CompareTo(c2.Name) < 0)
                        .Select(c2 => new { Actor1 = c1.Name, Actor2 = c2.Name, Movie = mc.Title })))
                .GroupBy(pair => new { pair.Actor1, pair.Actor2 })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key.Actor1} & {g.Key.Actor2}: {g.Count()} films");
            PrintResults("12. Популярные актерские дуэты", actorPairs);

            // 13. Индекс разнообразия
            var versatileCrew = movieCredits
                .SelectMany(mc => mc.Crew)
                .GroupBy(c => c.Name)
                .Select(g => new 
                {
                    Name = g.Key,
                    Departments = g.Select(c => c.Department).Distinct().Count(),
                    DepartmentsList = string.Join(", ", g.Select(c => c.Department).Distinct())
                })
                .OrderByDescending(x => x.Departments)
                .Take(5)
                .Select(x => $"{x.Name}: {x.Departments} departments ({x.DepartmentsList})");
            PrintResults("13. Самые разносторонние члены съемочной группы", versatileCrew);

            // 14. Творческие трио
            var tripleThreat = movieCredits
                .Where(mc => mc.Crew
                    .GroupBy(c => c.Name)
                    .Any(g => g.Select(c => c.Job)
                        .Intersect(new[] { "Director", "Writer", "Producer" })
                        .Count() == 3))
                .Select(mc => $"{mc.Title}: {string.Join(", ", mc.Crew
                    .GroupBy(c => c.Name)
                    .Where(g => g.Select(c => c.Job)
                        .Intersect(new[] { "Director", "Writer", "Producer" })
                        .Count() == 3)
                    .Select(g => g.Key))}");
            PrintResults("14. Фильмы с универсальными творцами", tripleThreat);

            // 15. Два шага до Кевина Бейкона
            var baconCoStars = movieCredits
                .Where(mc => mc.Cast.Any(c => c.Name == "Kevin Bacon"))
                .SelectMany(mc => mc.Cast.Select(c => c.Name))
                .Distinct();

            var twoStepsFromBacon = movieCredits
                .Where(mc => mc.Cast.Any(c => baconCoStars.Contains(c.Name)))
                .SelectMany(mc => mc.Cast.Select(c => c.Name))
                .Except(baconCoStars)
                .Where(name => name != "Kevin Bacon")
                .Distinct();
            PrintResults("15. Актеры в двух шагах от Кевина Бейкона", twoStepsFromBacon, 15);

            // 16. Анализ командной работы
            var directorStats = movieCredits
                .Where(mc => mc.Crew.Any(c => c.Job == "Director"))
                .GroupBy(mc => mc.Crew.First(c => c.Job == "Director").Name)
                .Select(g => new
                {
                    Director = g.Key,
                    AvgCastSize = g.Average(mc => mc.Cast.Count),
                    AvgCrewSize = g.Average(mc => mc.Crew.Count),
                    FilmCount = g.Count()
                })
                .OrderByDescending(x => x.FilmCount)
                .Select(x => $"{x.Director}: {x.FilmCount} films, avg cast: {x.AvgCastSize:F1}, avg crew: {x.AvgCrewSize:F1}");
            PrintResults("16. Статистика по режиссерам", directorStats, 15);

            // 17. Карьерные универсалы
            var versatileArtists = movieCredits
                .SelectMany(mc => mc.Cast.Select(cast => cast.Name)
                    .Intersect(mc.Crew.Select(crew => crew.Name)))
                .Distinct()
                .Select(name => new
                {
                    Name = name,
                    TopDepartment = movieCredits
                        .SelectMany(mc => mc.Crew)
                        .Where(c => c.Name == name)
                        .GroupBy(c => c.Department)
                        .OrderByDescending(g => g.Count())
                        .First()
                })
                .Select(x => $"{x.Name}: {x.TopDepartment.Key}");
            PrintResults("17. Универсальные таланты", versatileArtists, 15);

            // 18. Пересечение элитных клубов
            var eliteClubIntersection = movieCredits
                .Where(mc => mc.Crew.Any(c => c.Name == "Martin Scorsese" && c.Job == "Director"))
                .SelectMany(mc => mc.Cast.Select(c => c.Name).Union(mc.Crew.Select(c => c.Name)))
                .Intersect(movieCredits
                    .Where(mc => mc.Crew.Any(c => c.Name == "Christopher Nolan" && c.Job == "Director"))
                    .SelectMany(mc => mc.Cast.Select(c => c.Name).Union(mc.Crew.Select(c => c.Name))));
            PrintResults("18. Работали и со Скорсезе, и с Ноланом", eliteClubIntersection);

            // 19. Скрытое влияние департаментов
            var departmentInfluence = movieCredits
                .SelectMany(mc => mc.Crew
                    .Select(c => c.Department)
                    .Distinct()
                    .Select(dept => new 
                    {
                        Department = dept,
                        CastSize = mc.Cast.Count
                    }))
                .GroupBy(x => x.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    AvgCastSize = g.Average(x => x.CastSize),
                    MovieCount = g.Count()
                })
                .OrderByDescending(x => x.AvgCastSize)
                .Select(x => $"{x.Department}: avg cast size {x.AvgCastSize:F1} ({x.MovieCount} films)");
            PrintResults("19. Влияние департаментов на размер каста", departmentInfluence);

            // 20. Архетипы персонажей Джонни Деппа
            var deppArchetypes = movieCredits
                .SelectMany(mc => mc.Cast
                    .Where(c => c.Name == "Johnny Depp")
                    .Select(c => new 
                    { 
                        FirstWord = c.Character.Split(' ').FirstOrDefault() ?? "Unknown"
                    }))
                .GroupBy(x => x.FirstWord)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} roles ({string.Join(", ", g.Select(x => x.Character))})");
            PrintResults("20. Архетипы персонажей Джонни Деппа", deppArchetypes);
        }

        private static void PrintResults<T>(string header, IEnumerable<T> results)
        {
            Console.WriteLine($"\n=====================================\n");
            Console.WriteLine($"{header}:\n");

            foreach (var result in results) Console.WriteLine(result);
        }
        private static void PrintResults<T>(string header, IEnumerable<T> results, int topK)
        {
            Console.WriteLine($"\n=====================================\n");
            Console.WriteLine($"{header} (first {topK}):\n");

            foreach (var result in results.Take(topK)) Console.WriteLine(result);
        }
        private static void PrintResults<T>(string header, T result)
        {
            Console.WriteLine($"\n=====================================\n");
            Console.WriteLine($"{header}: {result}");
        }
    }
}