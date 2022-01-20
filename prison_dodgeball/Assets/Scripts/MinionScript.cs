﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MinionScript : MonoBehaviour {


    public PrisonDodgeballManager Mgr;

    NavMeshAgent agent;

    public Vector3 Velocity { get => agent.velocity; }
    public float MaxPathSpeed { get => agent.speed; }

    public int INTERNAL_SpawnIndex { get; set; }
    public int SpawnIndex { get => INTERNAL_SpawnIndex; }

    [SerializeField]
    Renderer BodyRenderer = null;

    [SerializeField]
    Renderer PantsRenderer = null;

    [SerializeField]
    Renderer HatTopRenderer = null;

    [SerializeField]
    Renderer HatBillRenderer = null;

    [SerializeField]
    TextMeshPro textMesh = null;

    [SerializeField]
    PrisonDodgeballManager.Team team;

    [SerializeField]
    bool reverseColor = false;

    public bool INTERNAL_ReverseColor
    {
        get
        {
            return reverseColor;
        }
        set
        {
            reverseColor = value;
            ConfigureForTeam();
        }
    }

    public bool ReverseColor { get => INTERNAL_ReverseColor; }

    public PrisonDodgeballManager.Team INTERNAL_Team
    { 
        get
        {
            return team;
        }
        set
        {
            team = value;

            ConfigureForTeam();
        }
    }

    public PrisonDodgeballManager.Team Team { get => INTERNAL_Team; }


    public bool INTERNAL_TouchingPrison { get; set; }

    public bool TouchingPrison { get => INTERNAL_TouchingPrison; }

    [SerializeField]
    Material LostMojoMaterial = null;
    [SerializeField]
    Material MojoMaterial = null;

    [SerializeField]
    Material TeamAMaterial = null;

    [SerializeField]
    Material TeamBMaterial = null;

    [SerializeField]
    GameObject BallPrefab = null;
    [SerializeField]
    GameObject BallHoldSpot = null;

    [SerializeField]
    Animator anim = null;

    [SerializeField]
    float throwSpeed = 20f;

    CapsuleCollider _collider;

    public float ThrowSpeed { get => throwSpeed; }

    public float Radius { get => _collider.radius; }

    public float Height { get => _collider.height; }

    [SerializeField]
    float maxAllowedOffAngleThrow = 20f;

    public float MaxAllowedOffAngleThrow { get => maxAllowedOffAngleThrow;  }

    [SerializeField]
    float turnToFaceSpeedDegPerSec = 20f;

    public float TurnToFaceSpeedDegPerSec { get => turnToFaceSpeedDegPerSec;  }

    [SerializeField]
    float dodgeSpeed = 10f;

    public float DodgeSpeed { get => dodgeSpeed;  }

    [SerializeField]
    float evadeCoolDownTimeSec = 0.4f;

    public float EvadeCoolDownTimeSec { get => evadeCoolDownTimeSec;  }

    float Epsilon = 0.001f;

    bool IsFacingTarget = false;
    Vector3 TargetPosToFace;

    float LastEvasion = 0f;

    DodgeBall HeldBall;

    public bool HasBall { get => HeldBall != null; }

    public int DodgeballIndex { get => HeldBall != null ? HeldBall.Index : -1; }

    public Vector3 HeldBallPosition { get => (HeldBall != null ? HeldBall.transform.position : Vector3.zero); }

    float lastTalk;
    float randomTalkWait;

    Rigidbody rbody;

    bool isPrisoner;

    public bool IsPrisoner
    {
        get
        {
            return isPrisoner;
        }

        private set
        {
            isPrisoner = value;

            if (isPrisoner)
            {
                if (INTERNAL_TeamLayerIndex == Mgr.MinionTeamALayerIndex)
                    this.gameObject.layer = Mgr.PrisonerTeamALayerIndex;
                else if (INTERNAL_TeamLayerIndex == Mgr.MinionTeamBLayerIndex)
                    this.gameObject.layer = Mgr.PrisonerTeamBLayerIndex;
                else
                    Debug.LogError("Unknown team!");

                // allow nav on the perp walk path
                agent.areaMask = agent.areaMask | (1 << teamPrisonNavMeshAreaIndex);

                //Debug.Log("NAVMASK: " + mgr.NavMeshMaskToString(agent.areaMask));
            }
            else
            {
                if (INTERNAL_TeamLayerIndex == Mgr.MinionTeamALayerIndex)
                    this.gameObject.layer = Mgr.MinionTeamALayerIndex;
                else if (INTERNAL_TeamLayerIndex == Mgr.MinionTeamBLayerIndex)
                    this.gameObject.layer = Mgr.MinionTeamBLayerIndex;
                else
                    Debug.LogError("Unknown team!");

            }                             
        }
    }


    public bool CanCollectBall { get => !IsPrisoner && !IsFreedPrisoner && !HasBall; }

    public bool CanBeRescued { get => IsPrisoner && INTERNAL_TouchingPrison; }

    int teamLayerIndex;

    public int INTERNAL_TeamLayerIndex
    {
        get => teamLayerIndex;
        set { teamLayerIndex = value; }
    }

    public int TeamLayerIndex { get => INTERNAL_TeamLayerIndex; }


    int INTERNAL_CurrentNavSurfaceMask { get; set; }
    int CurrentNavSurfaceMask { get => INTERNAL_CurrentNavSurfaceMask; }

    public enum EvasionDirection
    {
        Left,
        Right,
        Brake
    }

    int teamPrisonNavMeshAreaIndex;
    int teamNavMeshAreaIndex;


    bool wasPrisoner = false;
    float prisonReleaseTime;
    const float MaxPrisonReleaseTime = 10f;


    // Use this for initialization
    void Awake () {
		
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
            Debug.LogError("No agent");

        if (BodyRenderer == null)
            Debug.LogError("No body renderer");

        if (LostMojoMaterial == null)
            Debug.LogError("No lost mojo material");

        if (MojoMaterial == null)
            Debug.LogError("No mojo material");

        if (BallPrefab == null)
            Debug.LogError("Ball prefab is null");

        if (BallHoldSpot == null)
            Debug.LogError("Ball hold spot is null");

        if (TeamAMaterial == null)
            Debug.LogError("No team a material");

        if (TeamBMaterial == null)
            Debug.LogError("no team b material");

        if (HatBillRenderer == null)
            Debug.LogError("no hat bill renderer");
        if (HatTopRenderer == null)
            Debug.LogError("not hat top renderer");
        if (PantsRenderer == null)
            Debug.LogError("no pants renderer");

        if (textMesh == null)
            Debug.LogError("no text mesh");

        //anim = GetComponent<Animator>();
        if (anim == null)
            Debug.LogError("no anim");

        rbody = GetComponent<Rigidbody>();
        if (rbody == null)
            Debug.LogError("no rigidbody");

        _collider = GetComponent<CapsuleCollider>();
        if (_collider == null)
            Debug.LogError("no collider");

        //TeamLayerIndex = gameObject.layer;
    }


    void ConfigureForTeam()
    {
        var mgr = PrisonDodgeballManager.Instance;

        var baseMask = (1 << mgr.NeutralNavMeshAreaIndex) | (1 << mgr.WalkableNavMeshAreaIndex);

        if (INTERNAL_Team == PrisonDodgeballManager.Team.TeamA)
        {
            INTERNAL_TeamLayerIndex = mgr.MinionTeamALayerIndex;
            this.gameObject.layer = INTERNAL_TeamLayerIndex;

            var mat = reverseColor ? TeamBMaterial : TeamAMaterial;

            PantsRenderer.sharedMaterial = mat;
            HatTopRenderer.sharedMaterial = mat;
            HatBillRenderer.sharedMaterial = mat;

            teamPrisonNavMeshAreaIndex = mgr.TeamAPrisonNavMeshAreaIndex;
            teamNavMeshAreaIndex = mgr.TeamANavMeshAreaIndex;

            agent.areaMask = baseMask | (1 << mgr.TeamANavMeshAreaIndex);
        }
        else
        {
            INTERNAL_TeamLayerIndex = mgr.MinionTeamBLayerIndex;
            this.gameObject.layer = INTERNAL_TeamLayerIndex;

            var mat = reverseColor ? TeamAMaterial : TeamBMaterial;

            PantsRenderer.sharedMaterial = mat;
            HatTopRenderer.sharedMaterial = mat;
            HatBillRenderer.sharedMaterial = mat;

            teamPrisonNavMeshAreaIndex = mgr.TeamBPrisonNavMeshAreaIndex;
            teamNavMeshAreaIndex = mgr.TeamBNavMeshAreaIndex;

            agent.areaMask = baseMask | (1 << mgr.TeamBNavMeshAreaIndex);

        }
    }

 
    void Start() {

        if(Mgr == null)
            Mgr = PrisonDodgeballManager.Instance;

        lastTalk = Time.timeSinceLevelLoad;

        IsPrisoner = false;

        ConfigureForTeam();

    }

    public string NavMeshMaskToString()
    {
        return Mgr.NavMeshMaskToString(agent.areaMask);
    }

    public string NavMeshCurrentSurfaceToString()
    {
        return Mgr.NavMeshMaskToString(INTERNAL_CurrentNavSurfaceMask);
    }


    public void DisplayText(string s)
    {
        textMesh.text = s;
    }

    //public DodgeBall FindClosestBall()
    //{
    //    if (agent == null)
    //    {
    //        Debug.LogError("agent is null!");
    //        return null;
    //    }

    //    if (Mgr == null)
    //    {
    //        Debug.LogError("Mgr is null!");
    //        return null;
    //    }

    //    return Mgr.FindClosestDodgeBall(transform.position, agent.areaMask);
    //}

    //public bool IsTargetBallAvailable(DodgeBall db)
    //{
    //    return Mgr.IsDodgeBallAvailable(db, agent.areaMask);
    //}


    public void FaceTowards(Vector3 target)
    {
        IsFacingTarget = true;

        TargetPosToFace = target;
    }


    void FaceTowardsUpdate()
    {
        var target = TargetPosToFace;

        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));    // flattens the vector3
        var canRot = TurnToFaceSpeedDegPerSec * Time.deltaTime;
        //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, canRot); //will be clamped if overshoots
        rbody.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, canRot)); //will be clamped if overshoots)
    }

    public float SignedAngleWith(Vector3 target)
    {
        var forward = new Vector2(transform.forward.x, transform.forward.z);
        var relv = target - transform.position;
        var towards = new Vector2(relv.x, relv.z);

        return Vector2.SignedAngle(forward, towards);
    }

    public float AbsAngleWith(Vector3 target)
    {
        return Mathf.Abs(SignedAngleWith(target));
    }


    bool TargetSet = false;

    Vector3 GoToTarget;

    float MaxTargetDistDiff = 0.1f;

    public bool GoTo(Vector3 target)
    {
        if (TargetSet && agent.hasPath && !agent.isPathStale)
        {
            var dist = Vector3.Distance(GoToTarget, target);

            if (dist < MaxTargetDistDiff)
                return true;
        }

        IsFacingTarget = false;

        NavMeshHit nmh;
        if (NavMesh.SamplePosition(target, out nmh, agent.height * 2f, NavMesh.AllAreas))
        {
            if( agent.SetDestination(nmh.position))
            {
                TargetSet = true;

                GoToTarget = target;

                return true;
            }
            //else
            //{
            //    Debug.LogWarning("agent.SetDestination() failed");
            //}
        }
        //else
        //{
        //    Debug.LogWarning("NavMesh.SamplePosition() failed");
        //}

        return false;
    }

    public bool ReachedTarget()
    {
        return ( !agent.pathPending && 
            agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance);
    }

    public void Stop()
    {
        agent.ResetPath();
        TargetSet = false;
        IsFacingTarget = false;
    }


    public bool IsFreedPrisoner
    {
        get
        {
            if (!IsPrisoner)
            {
                if (0 != (INTERNAL_CurrentNavSurfaceMask & (1 << teamPrisonNavMeshAreaIndex)))
                {
                    return true;
                }
            }

            return false;
        }
    }


    private void Update()
    {


        if (NavMesh.SamplePosition(this.transform.position, out var hit, agent.height * 2f, agent.areaMask))
        {
            // We will use this to decide if agent is rescuable or hittable on way back after being rescued
            INTERNAL_CurrentNavSurfaceMask = hit.mask;

            if (!IsPrisoner && !IsFreedPrisoner)
            {
                var mask = (1 << teamPrisonNavMeshAreaIndex);
                if (0 != (agent.areaMask & mask))
                {
                    //Debug.LogError($"Locked out of prison area now! {mgr.NavMeshMaskToString(CurrentNavSurfaceMask)}");
                    agent.areaMask = agent.areaMask & ~mask;
                }
            }

        }


        //anim.SetFloat("speed", 0f);
        //Debug.Log($"Agent vel: {agent.velocity.magnitude}");

        anim.SetFloat("speed", agent.velocity.magnitude/agent.speed);

        if(IsFacingTarget)
        {
            FaceTowardsUpdate();
        }


        if ( Time.timeSinceLevelLoad - lastTalk > 3f + randomTalkWait)
        {
            lastTalk = Time.timeSinceLevelLoad;

            randomTalkWait = Random.Range(0f, 5f);

            EventManager.TriggerEvent<MinionJabberEvent, Vector3>(transform.position);
        }


        if(wasPrisoner && IsFreedPrisoner)
        {
            prisonReleaseTime = Time.timeSinceLevelLoad;
        }
        else if(!wasPrisoner && IsFreedPrisoner)
        {
            if(Time.timeSinceLevelLoad > prisonReleaseTime + MaxPrisonReleaseTime)
            {
                Debug.Log("Released prisoner took too long to leave. Back to prison!");
                INTERNAL_ReceiveBallHit();
            }
        }


        wasPrisoner = IsPrisoner;
    }


    public void INTERNAL_ReceiveBall(DodgeBall db)
    {

        //Debug.Log("Ball received!");

        if (db != null && !IsPrisoner && HeldBall == null)
        {
            //HeldBall = Instantiate(BallPrefab, BallHoldSpot.transform);
            HeldBall = db;

            HeldBall.IsHeld = true;

            HeldBall.transform.parent = this.BallHoldSpot.transform;
            HeldBall.transform.localPosition = Vector3.zero;
            HeldBall.transform.localRotation = Quaternion.identity;
            
        }
    }

    public bool Evade(EvasionDirection ed)
    {
        return Evade(ed, 1f);
    }

    public bool Evade(EvasionDirection ed, float strength)
    {
        strength = Mathf.Clamp(strength, 0f, 1f);

        if (Time.timeSinceLevelLoad < LastEvasion + EvadeCoolDownTimeSec)
            return false;

        LastEvasion = Time.timeSinceLevelLoad;

        switch(ed)
        {
            case EvasionDirection.Brake:
                Brake(strength);
                break;
            case EvasionDirection.Left:
                Dodge(true, strength);
                break;
            case EvasionDirection.Right:
                Dodge(false, strength);
                break;
        }

        return true;

    }


    void Dodge(bool isLeft, float strength)
    {
        var side = isLeft ? -transform.right : transform.right;

        var currSpeed = agent.velocity.magnitude;

        Vector3 currDir;
        if (currSpeed < Epsilon)
            currDir = agent.transform.forward;
        else
            currDir = agent.velocity / currSpeed;

        var newSpeed = currSpeed * 0.5f;


        agent.velocity = currDir * newSpeed + side * DodgeSpeed * strength;
    }


    void Brake(float strength)
    {
        var currSpeed = agent.velocity.magnitude;

        Vector3 currDir;
        if (currSpeed < Epsilon)
            currDir = agent.velocity;
        else
            currDir = agent.velocity / currSpeed;

        var reduction = 0.75f * strength;
        var newSpeed = currSpeed * (1f - reduction);

        agent.velocity = currDir * newSpeed;

    }


    public bool ThrowBall(Vector3 unitVDir, float normSpeed)
    {

        if (HeldBall == null || IsPrisoner || IsFreedPrisoner)
            return false;

        Vector3 velocity = unitVDir.normalized * throwSpeed * Mathf.Clamp(normSpeed, 0f, 1f);

        var unitVDir2d = new Vector2(unitVDir.x, unitVDir.z).normalized;

        var forward2d = new Vector2(transform.forward.x, transform.forward.z).normalized;

        var angle = Vector2.Angle(unitVDir2d, forward2d);

        if (angle > MaxAllowedOffAngleThrow)
            return false;
  
        HeldBall.INTERNAL_Throw(velocity);
        HeldBall = null;
        return true;
        
    }

    void DropBall()
    {
        if(HasBall)
        {
            HeldBall.SetToNeutral();

            var v = Vector3.up;

            var rotx = Quaternion.Euler(Random.Range(0f, 30f), 0f, 0f);
            var roty = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);

            HeldBall.INTERNAL_Throw(roty * rotx * Vector3.up * Random.Range(5f, 12f));

            HeldBall = null;
        }
    }


    public void INTERNAL_ReceiveBallHit()
    {

        if (IsPrisoner)
            return;

        IsPrisoner = true;

        DropBall();

        EventManager.TriggerEvent<MinionDeathEvent, Vector3, MinionScript>(this.transform.position + Vector3.up * 0.02f, this);

        EventManager.TriggerEvent<MinionOuchEvent, Vector3>(this.transform.position + Vector3.up * 0.02f);

        BodyRenderer.sharedMaterial = LostMojoMaterial;
    }


    
    public void INTERNAL_ReceiveRescue(DodgeBall db)
    {
        IsPrisoner = false;

        BodyRenderer.sharedMaterial = MojoMaterial;

        if(db != null)
            INTERNAL_ReceiveBall(db);
    }

}
