using Collections.Pooled;

namespace BlackStar.View;

public static class CaseNoBom
{
    const int NACT = 100;
    const int NRESOURCE = 70;
    const int STAGNATION = 10;
    const int POP = 10;
    static DateTime baseDt = new (2023, 1, 1);

    /// <summary>
    /// 无bom
    /// </summary>
    /// <returns></returns>
    public static async Task<Scene> OptimNoBom()
    {
        // 1. Generate 1000 random ActInt
        List<IAct> acts = new();
        JArray root = new();
        for (int i = 0; i < NACT; i++)
        {
            string name = $"act{i:000}";
            TimeSpan needTs = TimeSpan.FromMinutes(0.6 + Random.Shared.NextDouble());
            ActBool actInt = new(name) {NeedTs = new() { ["BoolService"] = needTs, }};
            //Console.WriteLine($"{name} need {actInt.NeedTs}");
            acts.Add(actInt);
            root.Add(new JObject
                {
                    ["需求名称"] = name,
                    ["需求加工时间"] = needTs.TotalMinutes,
                });
        }
        Console.WriteLine($"need total {acts.Sum(i => ((ActBool)i).NeedTs["BoolService"].TotalMinutes)}");
        
        File.WriteAllText("require.json", root.ToString());

        // 2. Generate 2000 Resource with State<bool>
        PooledDictionary<string, IResource> resources = new();
        root = new();
        for (int i = 0; i < NRESOURCE; i++)
        {
            string name = $"res{i:000}";
            Resource<bool> resource = new Resource<bool>(name);
            var statestart = baseDt + TimeSpan.FromMinutes(20 * Random.Shared.NextDouble());
            var stateend = statestart + TimeSpan.FromMinutes(2 + 2.5 * Random.Shared.NextDouble());
            State<bool> state = new State<bool>("BoolService", statestart, stateend, true);
            resource.States = new PooledList<State<bool>> { state };
            //Console.WriteLine($"{name} provide {state.To- state.From}");
            resources.TryAdd(name, resource);
            root.Add(new JObject()
            {
                ["机器名称"] = name,
                ["可用时间开始"] = statestart,
                ["可用时间结束"] = stateend,
            });
        }
        var provideTotal = resources.Sum(
            i => ((Resource<bool>)i.Value).States
                .Sum(j => (j.To - j.From).TotalMinutes));
        Console.WriteLine($"provides  total {provideTotal}");
        Console.WriteLine();
        File.WriteAllText("machine.json", root.ToString());
        SortAllTransolution solver = new(acts, resources, pop: POP, stagnation: STAGNATION);
        Scene scene = null;
        await Task.Run(async () =>
        {
            scene = await solver.Solve();
        });

        if (scene != null)
            Debug.WriteLine($"use resources {scene.Deploys.Select(i => i.UseResource).Distinct().Count()}");

        return scene;
    }

}