using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ShootingRange : MonoBehaviour
{

    int ProjectilePoolSize = 40;

    List<Projectile> ProjectilePool = new List<Projectile>();

    public Projectile ProjectilePrefab;

    public MovingTarget Target;


    public float StartTime = 0f;

    public float CoolOffTime = 0.25f;

    float LastShotTime = 0f;

    public Vector3 LaunchPos = new Vector3(0f, 1f, 0f);

    public float ShotSpeed = 14f;
    float OrigShotSpeed = 14f;

    int hits = 0;
    int misses = 0;

    public TextMeshProUGUI HitsText;
    public TextMeshProUGUI MissesText;
    public TextMeshProUGUI AvgText;
    public TextMeshProUGUI SPSText;
    public TextMeshProUGUI AlgText;

    public GameObject LaunchSpotIndicator;

    public Vector2 LaunchHeightRange = new Vector2(1f, 15f);


    public float MaxAbsLauncherHeightChangeVel = 3f;
    public float LaunchHeightRandomAccel = 0.5f;

    float launcherVel = 0f;



    delegate bool AimMethodCallback(Vector3 projectilePos, float maxProjectileSpeed, Vector3 projectileGravity,
    Vector3 targetInitPos, Vector3 targetConstVel, Vector3 targetForwardDir,
    float maxAllowedErrorDist,
    out Vector3 projectileDir, out float projectileSpeed, out float interceptT, out float altT);


    public float DB_launchAngle = 0f;

    struct AimMethod
    {
        public string Name { get; private set; }
        public AimMethodCallback Callback { get; private set; }
        public Vector3 ProjectileGravity { get; private set; }

        public AimMethod(string name, AimMethodCallback cbk):this(name, cbk, Vector3.zero)
        {
        }

        public AimMethod(string name, AimMethodCallback cbk, Vector3 projectileGravity)
        {
            Name = name;
            Callback = cbk;
            ProjectileGravity = projectileGravity;
        }

    }


    List<AimMethod> AimMethods = new List<AimMethod>();

    AimMethod CurrentAimMethod;

    int currentAimMethodIndex = 0;

    float maxAllowedErrorDist = (0.25f + 0.5f) * 0.99f; //projectile radius + target radius 

    private void Awake()
    {
        if(ProjectilePrefab == null)
        {
            Debug.LogError("No projectile prefab");
        }

        if(Target == null)
        {
            Debug.LogError("No target");
        }

        if(HitsText == null)
        {
            Debug.LogError("no hits text");
        }
        if (MissesText == null)
        {
            Debug.LogError("misses text");
        }
        if (AvgText == null)
        {
            Debug.LogError("avg text");
        }
        if(SPSText == null)
        {
            Debug.LogError("no sps text");
        }

        if(AlgText == null)
        {
            Debug.LogError("No alg text");
        }


        if(LaunchSpotIndicator == null)
        {
            Debug.LogError("launch spot indicator is null");
        }

        for (int i = 0; i < ProjectilePoolSize; ++i)
        {

            var prj = Instantiate<Projectile>(ProjectilePrefab);
            prj.gameObject.SetActive(false);
            prj.mgr = this;

            ProjectilePool.Add(prj);
        }


        AimMethods.Add(new AimMethod("StudentMinion.PredictThrow", MinionStateMachine.PredictThrow, Physics.gravity));
        AimMethods.Add(new AimMethod("GlassJoe.PredictThrow", MinionBasicDemoStateMachine.PredictThrow, Physics.gravity));
        

        CurrentAimMethod = AimMethods[currentAimMethodIndex];
    }


    // Start is called before the first frame update
    void Start()
    {
        OrigShotSpeed = ShotSpeed;

        ResetStats();
    }

    void ResetStats()
    {
        AvgText.text = "0";
        HitsText.text = "0";
        MissesText.text = "0";
        SPSText.text = "0.0";
        AlgText.text = CurrentAimMethod.Name; //predictionAlg.ToString();
        StartTime = Time.timeSinceLevelLoad;

        hits = 0;
        misses = 0;
    }

    void PrintAvg()
    {
        AvgText.text = (100f*hits / (float)(hits + misses)).ToString("0.");
    }

    void PrintSPS()
    {
        var elapsed = Time.timeSinceLevelLoad - StartTime;

        var totShots = hits + misses;

        var sps = totShots / elapsed;

        SPSText.text = (60.0f * sps).ToString("0.00");
    }

    public void RecieveHit()
    {
        ++hits;
        HitsText.text = hits.ToString();
        PrintAvg();
    }

    public void RecieveMiss()
    {
        ++misses;
        MissesText.text = misses.ToString();
        PrintAvg();
    }


    public void Recycle(Projectile p)
    {
        ProjectilePool.Add(p);
    }


    private void Update()
    {
        PrintSPS();

        if(Input.GetKeyUp(KeyCode.Space))
        {

            ++currentAimMethodIndex;

            if (currentAimMethodIndex >= AimMethods.Count)
            {
                currentAimMethodIndex = 0;  
            }

            CurrentAimMethod = AimMethods[currentAimMethodIndex];

            Physics.gravity = AimMethods[currentAimMethodIndex].ProjectileGravity;

            ResetStats();
        }

        if(Input.GetKeyUp(KeyCode.R))
        {
            ResetStats();
        }

        if(Input.GetKeyUp(KeyCode.Alpha1))
        {
            LaunchHeightRange = new Vector2(1f, 15f);
            Target.SpeedRange = new Vector2(4f, 15f);
            Target.SetSpeed(4f);
            Target.YRange = new Vector2(1f, 15f);
            ShotSpeed = OrigShotSpeed;
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            LaunchHeightRange = new Vector2(1f, 1f);
            Target.SpeedRange = new Vector2(0f, 0f);
            Target.SetSpeed(0f);
            Target.YRange = new Vector2(1f, 15f);
            Target.transform.position = new Vector3(4f, 1f, 6f);
            ShotSpeed = OrigShotSpeed;
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            LaunchHeightRange = new Vector2(1f, 15f);
            Target.SpeedRange = new Vector2(1f, 1f);
            Target.SetSpeed(1f);
            Target.YRange = new Vector2(1f, 15f);
            ShotSpeed = OrigShotSpeed;
        }

        if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            // Prison Dodgball Mode
            var mh = 0.7126667f;
            var h = mh + 0.694f; // minion height + ball hold spot offset
            LaunchHeightRange = new Vector2(h, h);
            var s = 8f;
            Target.SpeedRange = new Vector2(s, s);
            Target.YRange = new Vector2(mh, mh);
            Target.SetSpeed(s);
            ShotSpeed = 20f;
        }
    }


    void FixedUpdate()
    {

        // The launcher randomly moves up and down for variety of shot angles
        var h = LaunchPos.y;

        var r = Random.Range(-1f, 1f) * Random.Range(-1f, 1f);

        r *= Time.deltaTime * LaunchHeightRandomAccel;

        launcherVel = Mathf.Clamp(launcherVel + r, -MaxAbsLauncherHeightChangeVel, MaxAbsLauncherHeightChangeVel);

        h += launcherVel;

        h = Mathf.Clamp(h, LaunchHeightRange.x, LaunchHeightRange.y);

        if (h == LaunchHeightRange.x || h == LaunchHeightRange.y)
            launcherVel = 0f;

        LaunchPos.y = h;

        LaunchSpotIndicator.transform.position = LaunchPos;

        if (ProjectilePool.Count > 0 && Time.timeSinceLevelLoad > (LastShotTime + CoolOffTime))
        {

            // Calc the shot trajectory, if possible
            if(CurrentAimMethod.Callback(LaunchPos, ShotSpeed, Physics.gravity,
                Target.transform.position, Target.rbody.velocity, Target.transform.forward,
                maxAllowedErrorDist,
                out var projectileDir, out var projectileSpeed, out var t, out var altT))
            {


                // Don't fire if the projectile is going to bounce off the imaginary wall before shot
                // can get there
                var intercept = Target.transform.position + Target.rbody.velocity * t;

                if (Mathf.Abs(intercept.x) <= Target.AbsXRange)
                {

                    // FIRE!

                    var xzDir = (new Vector3(projectileDir.x, 0f, projectileDir.z)).normalized;
                    DB_launchAngle = Vector3.Angle(xzDir, projectileDir);
                    // The following uses object recycling
                    Projectile p = ProjectilePool[0];

                    ProjectilePool.RemoveAt(0);

                    p.gameObject.SetActive(true);

                    p.gameObject.transform.position = LaunchPos;
                    p.gameObject.transform.rotation = Quaternion.identity;

                    p.rbody.ResetInertiaTensor();
                    p.rbody.velocity = Vector3.zero;
                    p.rbody.angularVelocity = Vector3.zero;

                    var throwVec = projectileDir * projectileSpeed;

                    p.rbody.AddForce(throwVec, ForceMode.VelocityChange);

                    LastShotTime = Time.timeSinceLevelLoad;
                }
            }

        }

    }

}
