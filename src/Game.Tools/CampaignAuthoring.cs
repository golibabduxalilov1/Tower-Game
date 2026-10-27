using TowerGame.Content;
using TowerGame.Core;

namespace TowerGame.Tools;

/// <summary>Original coordinate-based authoring; export commits the resulting campaign, not random seeds.</summary>
public static class CampaignAuthoring
{
    public static ContentPack Create()
    {
        // The latest user reference specifies sixteen columns and twelve rows.
        // Coordinates are authored afresh instead of downsampling old routes/obstacles.
        // The user requested stone-free fields; no campaign cell is authored as Rock.
        var pack = new ContentPack { ContentVersion = "3.2.0" };
        var valley = New("valley", "Green Valley", "Yashil vodiy", "green", "Build your first maze in an open meadow.", TowerKind.Flame, TowerKind.Ice);
        Ground(valley, "west", new(0,6), new(15,6));
        Air(valley, "sky", new(0,3), new(8,3), new(15,3));
        pack.Maps.Add(valley);

        var twin = New("twin-streams", "Twin Streams", "Ikki oqim", "river", "Two crossing fronts share a central firing zone.", TowerKind.Laser, TowerKind.Ice);
        Ground(twin, "west", new(0,6), new(15,6));
        Ground(twin, "north", new(8,0), new(8,11));
        Air(twin, "sky", new(0,0), new(8,6), new(15,11));
        pack.Maps.Add(twin);

        var sand = New("sand-fortress", "Sand Fortress", "Qum qal’asi", "sand", "Split your defense between two gates and an independent air lane.", TowerKind.Mortar, TowerKind.Laser);
        Ground(sand, "upper", new(0,3), new(15,6));
        Ground(sand, "lower", new(0,9), new(15,6));
        Air(sand, "sky", new(0,0), new(8,2), new(15,0));
        pack.Maps.Add(sand);

        var crystal = New("crystal-mine", "Crystal Mine", "Kristall kon", "crystal", "A mine railway runs above the ground front.", TowerKind.Laser, TowerKind.Mortar);
        Ground(crystal, "west", new(0,8), new(15,8));
        Air(crystal, "sky", new(0,5), new(8,6), new(15,5));
        Rail(crystal, "rail", new(0,2), new(5,2), new(5,0), new(11,0), new(11,2), new(15,2));
        pack.Maps.Add(crystal);

        var stone = New("stone-gates", "Stone Gates", "Tosh darvozalar", "stone", "Build your own choke points across the open field.", TowerKind.Flame, TowerKind.Mortar);
        Ground(stone, "west", new(0,6), new(15,6));
        Air(stone, "sky", new(0,2), new(8,5), new(15,9));
        pack.Maps.Add(stone);

        var ice = New("ice-core", "Ice Core", "Muz markazi", "ice", "Protect the central reactor from four approaching fronts.", TowerKind.Ice, TowerKind.Laser);
        Ground(ice, "west", new(0,6), new(8,6));
        Ground(ice, "east", new(15,6), new(8,6));
        Ground(ice, "north", new(8,0), new(8,6));
        Ground(ice, "south", new(8,11), new(8,6));
        Air(ice, "sky", new(0,0), new(5,3), new(8,6));
        pack.Maps.Add(ice);

        var marsh = New("marsh-border", "Marsh Border", "Botqoq chegarasi", "marsh", "Raised building pads overlook a winding causeway and railway.", TowerKind.Flame, TowerKind.Laser);
        Array.Fill(marsh.Cells, CellType.Ground);
        Carve(marsh, CellType.Path, new(0,8), new(3,8), new(3,5), new(8,5), new(8,9), new(12,9), new(12,6), new(15,6));
        // Open earth lets the player reroute enemies around the causeway.
        Fill(marsh, CellType.Ground, 1,6,3,4);
        Fill(marsh, CellType.Ground, 11,6,4,4);
        // Raised pads flank the railway and the winding causeway; they do not alter either route.
        foreach (var p in new[] { new Cell(2,3),new Cell(5,3),new Cell(8,3),new Cell(11,3),new Cell(14,3),new Cell(2,11),new Cell(5,11),new Cell(8,11),new Cell(11,11),new Cell(14,11),new Cell(6,6),new Cell(6,8),new Cell(9,5),new Cell(9,6),new Cell(9,8),new Cell(15,5) })
            marsh.Cells[p.Y*marsh.Width+p.X] = CellType.Platform;
        for (int x=0;x<marsh.Width;x++) marsh.Cells[x] = CellType.Platform;
        Ground(marsh, "west", new(0,8), new(15,6));
        Air(marsh, "sky", new(0,5), new(8,6), new(15,5));
        Rail(marsh, "rail", new(0,2), new(15,2));
        pack.Maps.Add(marsh);

        var volcano = New("volcanic-ring", "Volcanic Ring", "Vulkan halqasi", "volcano", "Balance ground, air and two independent railway fronts.", TowerKind.Mortar, TowerKind.Ice);
        Ground(volcano, "west", new(0,5), new(15,5));
        Ground(volcano, "east", new(15,6), new(0,6));
        Air(volcano, "sky", new(0,3), new(8,6), new(15,9));
        Rail(volcano, "rail-north", new(0,0), new(15,0));
        Rail(volcano, "rail-south", new(15,11), new(0,11));
        pack.Maps.Add(volcano);

        for (int i=0;i<pack.Maps.Count;i++)
        {
            var map = pack.Maps[i];
            // Persist this once: both authored campaign waves and seeded Endless use the same scale.
            map.HealthScale = map.Id == "marsh-border" ? .80 : map.Routes.Any(r => r.Movement == MovementKind.Rail) ? .85 : 1.0;
            map.Waves = Enumerable.Range(1,100).Select(w => Wave(map,w,i)).ToList();
        }
        return pack;
    }

    static MapDefinition New(string id,string name,string uz,string theme,string description,TowerKind a,TowerKind b) =>
        new() { Id=id,Name=name,NameUz=uz,Theme=theme,Description=description,Width=16,Height=12,Cells=new CellType[192],SpecialTowers=[a,b] };
    static void Fill(MapDefinition m,CellType type,int x,int y,int w,int h)
    { for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)m.Cells[yy*m.Width+xx]=type; }
    static void Ground(MapDefinition m,string id,Cell a,Cell b)
    {
        m.Routes.Add(new(){Id=id,Movement=MovementKind.Ground,Points=[a,b]});
        m.Cells[a.Y*m.Width+a.X]=CellType.Entrance;m.Cells[b.Y*m.Width+b.X]=CellType.Exit;
    }
    static void Air(MapDefinition m,string id,params Cell[] points) =>
        m.Routes.Add(new(){Id=id,Movement=MovementKind.Air,Points=points.ToList()});
    static List<Cell> Expand(Cell[] corners)
    {
        List<Cell> points=[corners[0]];
        foreach(var target in corners.Skip(1))
        {
            var current=points[^1];
            while(current!=target)
            {
                current = current.X!=target.X ? new(current.X+Math.Sign(target.X-current.X),current.Y) : new(current.X,current.Y+Math.Sign(target.Y-current.Y));
                points.Add(current);
            }
        }
        return points;
    }
    static void Carve(MapDefinition m,CellType type,params Cell[] points)
    { foreach(var p in Expand(points))m.Cells[p.Y*m.Width+p.X]=type; }
    static void Rail(MapDefinition m,string id,params Cell[] corners)
    {
        var points=Expand(corners);foreach(var p in points)m.Cells[p.Y*m.Width+p.X]=CellType.Rail;
        m.Routes.Add(new(){Id=id,Movement=MovementKind.Rail,Points=points});
    }

    static WaveDefinition Wave(MapDefinition map,int w,int mapIndex)
    {
        var wave=new WaveDefinition {Number=w,Reward=0};
        int count=Math.Min(60,8+(int)Math.Floor(.6*w));
        // A smaller board has shorter flight/rail exposure and fewer construction cells.
        // Keep tower economics intact; scale campaign health and spread spawns modestly instead.
        double hp=map.HealthScale*(1+.075*(w-1))*Math.Pow(1.012,w-1), interval=Math.Max(.30,.85-.004*w);
        var eligible=map.Routes.Where(r=>r.Movement==MovementKind.Ground || w>= (r.Movement==MovementKind.Air?6:8)).ToArray();
        for(int i=0;i<count;i++)
        {
            var route=eligible[i%eligible.Length];
            // Introduce air as scouts before mixing sustained air/ground pressure.
            if (w < 13 && route.Movement == MovementKind.Air && i % 4 != 3)
            {
                var ground = eligible.Where(r => r.Movement == MovementKind.Ground).ToArray();
                route = ground[i % ground.Length];
            }
            EnemyKind kind;
            if(w%10==0&&i==count/2)
            {
                route=map.Routes[(w/10-1)%map.Routes.Count];
                kind=route.Movement switch {MovementKind.Air=>EnemyKind.AirFortress,MovementKind.Rail=>EnemyKind.RailColossus,_=>EnemyKind.HeavyTank};
            }
            else kind=route.Movement switch
            {
                MovementKind.Air => w>=14&&(i+w)%3==0?EnemyKind.Plane:EnemyKind.Helicopter,
                MovementKind.Rail => EnemyKind.RailCart,
                _ => GroundKind(w,i+mapIndex)
            };
            wave.Groups.Add(new(){Kind=kind,Count=1,RouteId=route.Id,Delay=Math.Round(Balance.SpawnTime(i,interval),6),Interval=interval,HealthMultiplier=hp});
        }
        return wave;
    }
    static EnemyKind GroundKind(int wave,int index)
    {
        if(wave<3)return EnemyKind.Infantry;
        if(wave<5)return index%3==0?EnemyKind.Runner:EnemyKind.Infantry;
        if(wave<9)return (EnemyKind)(index%3);
        if(wave<16)return (EnemyKind)(index%4);
        return (EnemyKind)(index%5);
    }
}
