using Collections.Pooled;

OptimTest1(50,60);

void OptimTest1(int nAct, int nResource)
{
    DateTime start = new DateTime(2023, 1, 1);

    // 1. Generate 1000 random ActInt
    List<IAct> acts = new();
    for (int i = 0; i < nAct; i++)
    {
        string name = $"act{i:000}";
        ActBool actInt = new(name)
        {
            NeedTs = new () { ["BoolService"] = TimeSpan.FromMinutes(0.6 + Random.Shared.NextDouble())},
        };
        //Console.WriteLine($"{name} need {actInt.NeedTs}");
        acts.Add(actInt);
    }
    Console.WriteLine($"need total {acts.Sum(i=> ((ActBool)i).NeedTs["BoolService"].TotalMinutes)}");
    //Console.WriteLine();

    // 2. Generate 2000 Resource with State<bool>
    PooledDictionary<string, IResource> resources = new();
    for (int i = 0; i < nResource; i++)
    {
        string name = $"res{i:000}";
        Resource<bool> resource = new Resource<bool>(name);
        var statestart = new DateTime(2023, 1, 1) + TimeSpan.FromMinutes(20 * Random.Shared.NextDouble());
        var stateend = statestart + TimeSpan.FromMinutes(2 + 2.5 * Random.Shared.NextDouble());
        State<bool> state = new State<bool>("BoolService", statestart, stateend, true);
        resource.States = new PooledList<State<bool>> { state };
        //Console.WriteLine($"{name} provide {state.To- state.From}");
        resources.TryAdd(name, resource);
    }
    var provideTotal = resources.Sum(
        i => ((Resource<bool>)i.Value).States
            .Sum(j => (j.To - j.From).TotalMinutes));
    Console.WriteLine($"provides  total {provideTotal}");
    Console.WriteLine();
    SortAllSolver solver = new();
    var scene = solver.Solve(acts, resources);

}