using System.Text.Json;
using TowerGame.Content;
using TowerGame.Core;
using TowerGame.Tools;

if (args.Contains("--combat-audit")) { CombatAudit.Run(); return 0; }

if(args.Contains("--assets"))
{
    var path=args.SkipWhile(a=>a!="--assets").Skip(1).FirstOrDefault() ?? "docs/store/assets";
    StoreArt.Generate(path);
    Console.WriteLine($"Generated original vector icon and capsule draft in {path}.");
    return 0;
}

if(args.Contains("--trace"))
{
    // --trace <map> <difficulty> [mode]: per-wave escapes, money and towers for balance diagnosis.
    var rest=args.SkipWhile(a=>a!="--trace").Skip(1).ToArray();
    var game=new GameSession(Catalog.Get(rest[0]),rest.Length>2?Enum.Parse<Mode>(rest[2]):Mode.Classic,Enum.Parse<Difficulty>(rest[1]));
    var bot=new AutoPlayer(game);
    var escaped=new Dictionary<EnemyKind,int>();
    game.EnemyEscaped+=e=>escaped[e.Kind]=escaped.GetValueOrDefault(e.Kind)+1;
    while(game.Wave<100)
    {
        escaped.Clear();
        var alive=bot.PlayWave();
        var kinds=string.Join(",",game.GetWave(game.Wave).Groups.GroupBy(g=>g.Kind).Select(g=>$"{g.Key}x{g.Sum(x=>x.Count)}"));
        Console.WriteLine($"w{game.Wave,3} lives {game.Lives,2} money {game.Money,5} towers {game.Towers.Count,3} [{string.Join(",",game.Towers.GroupBy(t=>t.Kind).Select(g=>$"{g.Key}:{g.Count()}"))}] escaped {string.Join(",",escaped.Select(p=>$"{p.Key}x{p.Value}"))} | {kinds}");
        if(!alive)break;
    }
    return 0;
}

if(args.Contains("--balance"))
{
    // Every map x difficulty played to the end by the deterministic AutoPlayer.
    var mode=Enum.Parse<Mode>(args.SkipWhile(a=>a!="--balance").Skip(1).FirstOrDefault(a=>!a.StartsWith("--")) ?? "Classic");
    var only=args.SkipWhile(a=>a!="--map").Skip(1).FirstOrDefault();
    var jobs=Catalog.Maps.Where(m=>only==null||m.Id==only).SelectMany(m=>Enum.GetValues<Difficulty>().Select(d=>(Map:m,Difficulty:d))).ToList();
    var results=new System.Collections.Concurrent.ConcurrentDictionary<int,string>();
    var wins=0;
    Parallel.For(0,jobs.Count,i=>
    {
        var (map,difficulty)=jobs[i];
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var game=new GameSession(map,mode,difficulty);
        var bot=new AutoPlayer(game);
        var limit=mode==Mode.Endless?150:100;
        while(game.Wave<limit&&bot.PlayWave()){}
        // Endless has no victory: its result is the wave reached (capped at 150 for runtime).
        var won=game.State==SessionState.Victory||mode==Mode.Endless&&game.Wave>=limit&&game.Lives>0;
        if(won)Interlocked.Increment(ref wins);
        var verdict=mode==Mode.Endless?(won?"CAP ":"END "):(won?"WIN ":"LOSS");
        results[i]=$"{map.Id,-15} {difficulty,-6} {verdict} wave {game.Wave,3} lives {game.Lives,2} towers {game.Towers.Count,3} L3 {game.Towers.Count(t=>t.Level==3),3} score {game.Score,9} ({watch.Elapsed.TotalSeconds:0.0}s)";
    });
    foreach(var line in results.OrderBy(r=>r.Key))Console.WriteLine(line.Value);
    if(mode==Mode.Endless){Console.WriteLine($"Endless: {wins}/{jobs.Count} reached the wave-{150} cap; others list the wave where the run ended.");return 0;}
    Console.WriteLine($"{mode}: {wins}/{jobs.Count} won by AutoPlayer");
    return wins==jobs.Count?0:2;
}

if(args.Contains("--generate"))
{
    var pack=CampaignAuthoring.Create();
    var errors=ContentValidator.Validate(pack);
    if(errors.Count>0){foreach(var e in errors)Console.Error.WriteLine(e);return 1;}
    var path=args.SkipWhile(a=>a!="--generate").Skip(1).FirstOrDefault() ?? "content/campaign.json";
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path,JsonSerializer.Serialize(pack,Catalog.JsonOptions));
    Console.WriteLine($"Exported {pack.Maps.Count} maps / {pack.Maps.Sum(m=>m.Waves.Count)} waves to {path}.");
    return 0;
}

var maps=Catalog.Maps;
var issues=ContentValidator.Validate(new(){Maps=maps.ToList()});
foreach(var issue in issues)Console.Error.WriteLine(issue);
if(issues.Count>0)return 1;
foreach(var map in maps)
{
    var session=new GameSession(map);
    var ground=map.Routes.Where(r=>r.Movement==MovementKind.Ground);
    Console.WriteLine($"{map.Id,-18} {map.Waves.Count} waves, {map.Routes.Count} routes, paths {string.Join("/",ground.Select(r=>session.PreviewPath(r.Id).Count-1))} cells");
}
Console.WriteLine("PASS: eight maps; 800 waves; all routes, all enemies, all towers; boss schedule, rewards and population caps.");
return 0;
