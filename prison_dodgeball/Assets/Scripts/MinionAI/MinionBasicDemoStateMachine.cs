using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using GameAI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(MinionScript))]
public class MinionBasicDemoStateMachine : MonoBehaviour
{
    public const string StudentName = "Glass Joe";

    public const string GlobalTransitionStateName = "GlobalTransition";
    public const string CollectBallStateName = "CollectBall";
    public const string GoToThrowSpotStateName = "GoToThrowBall";
    public const string ThrowBallStateName = "ThrowBall";
    public const string DefensiveDemoStateName = "DefensiveDemo";
    public const string GoToPrisonStateName = "GoToPrison";
    public const string LeavePrisonStateName = "LeavePrison";
    public const string GoHomeStateName = "GoHome";
    public const string RescueStateName = "Rescue";
    public const string RestStateName = "Rest";


    // For throws...
    public static float MaxAllowedThrowPositionError = (0.25f + 0.5f) * 0.99f;

    // Data that each FSM state gets initialized with (passed as init param)
    FiniteStateMachine<MinionFSMData> fsm;

    public MinionScript Minion { get; private set; }

    PrisonDodgeballManager Mgr;
    public TeamShare TeamData { get; private set; }

    struct MinionFSMData
    {
        public MinionBasicDemoStateMachine MinionFSM { get; private set; }
        public MinionScript Minion { get; private set; }
        public PrisonDodgeballManager Mgr { get; private set; }
        public PrisonDodgeballManager.Team Team { get; private set; }
        public TeamShare TeamData { get; private set; }

        public MinionFSMData(
            MinionBasicDemoStateMachine minionFSM,
            MinionScript minion,
            PrisonDodgeballManager mgr,
            PrisonDodgeballManager.Team team,
            TeamShare teamData
            )
        {
            MinionFSM = minionFSM;
            Minion = minion;
            Mgr = mgr;
            Team = team;
            TeamData = teamData;
        }
    }


    // Turn to face target, but taking into account hand offset
    public static Vector3 FindTurnTowardsTargetForAim(Vector3 targetPos, Vector3 minionPos, Vector3 minionForward, float holdSpotOffset)
    {
        var targetPos2d = new Vector2(targetPos.x, targetPos.z);
        var minionPos2d = new Vector2(minionPos.x, minionPos.z);

        var minionToTarget2d = targetPos2d - minionPos2d;

        float distMinionFromTarget = minionToTarget2d.magnitude;

        var angle = Mathf.Atan2(holdSpotOffset, distMinionFromTarget);

        //Debug.Log($"Rotating by {angle * Mathf.Rad2Deg} degrees");

        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        // Rotate
        var newMinionToTarget2d = new Vector2(
            minionToTarget2d.x * cos - minionToTarget2d.y * sin,
            minionToTarget2d.x * sin + minionToTarget2d.y * cos);

        return new Vector3(minionPos2d.x + newMinionToTarget2d.x, targetPos.y, minionPos2d.y + newMinionToTarget2d.y);
    }







    // Note: You have to implement the following method with prediction:
    // Either directly solved (e.g. Law of Cosines or similar) or iterative.
    // You cannot modify the method signature. However, if you want to do more advanced
    // prediction (such as analysis of the navmesh) then you can make another method that calls
    // this one. 
    // Be sure to run the editor mode unit test to confirm that this method runs without
    // any gamemode-only logic
    public static bool PredictThrow(
        // The initial launch position of the projectile
        Vector3 projectilePos,
        // The initial ballistic speed of the projectile
        float maxProjectileSpeed,
        // The gravity vector affecting the projectile (likely passed as Physics.gravity)
        Vector3 projectileGravity,
        // The initial position of the target
        Vector3 targetInitPos,
        // The constant velocity of the target (zero acceleration assumed)
        Vector3 targetConstVel,
        // The forward facing direction of the target. Possibly of use if the target
        // velocity is zero
        Vector3 targetForwardDir,
        // For algorithms that approximate the solution, this sets a limit for how far
        // the target and projectile can be from each other at the interceptT time
        // and still count as a successful prediction
        float maxAllowedErrorDist,
        // Output param: The solved projectileDir for ballistic trajectory that intercepts target
        out Vector3 projectileDir,
        // Output param: The speed the projectile is launched at in projectileDir such that
        // there is a collision with target. projectileSpeed must be <= maxProjectileSpeed
        out float projectileSpeed,
        // Output param: The time at which the projectile and target collide
        out float interceptT,
        // Output param: An alternate time at which the projectile and target collide
        // Note that this is optional to use and does NOT coincide with the solved projectileDir
        // and projectileSpeed. It is possibly useful to pass on to an incremental solver.
        // It only exists to simplify compatibility with the ShootingRange
        out float altT)
    {
        // TODO implement an accurate throw with prediction. This is just a placeholder

        // FYI, if Minion.transform.position is sent via param targetPos,
        // be aware that this is the midpoint of Minion's capsuleCollider
        // (Might not be true of other agents in Unity though. Just keep in mind for future game dev)

        // Only going 2D for simple demo. this is not useful for proper prediction
        // Basically, avoiding throwing down at enemies since we aren't predicting accurately here.
        var targetPos2d = new Vector3(targetInitPos.x, 0f, targetInitPos.z);
        var launchPos2d = new Vector3(projectilePos.x, 0f, projectilePos.z);

        var relVec = (targetPos2d - launchPos2d);
        interceptT = relVec.magnitude / maxProjectileSpeed;
        altT = -1f;

        // This is a hard-coded approximate sort of of method to figure out a loft angle
        // This is NOT the right thing to do for your prediction code!
        var normAngle = Mathf.Lerp(0f, 20f, interceptT * 0.007f);
        var v = Vector3.Slerp(relVec.normalized, Vector3.up, normAngle);

        // Make sure this is normalized! (The direction of your throw)
        projectileDir = v;

        // You'll probably want to leave this as is. For advanced prediction you can slow your throw down
        // You don't need to predict the speed of your throw. Only the direction assuming full speed
        projectileSpeed = maxProjectileSpeed;

        // TODO return true or false based on whether target can actually be hit
        // This implementation just thinks, "I guess so?", and returns true
        // Implementations that don't exactly solve intercepts will need to test the approximate
        // solution with maxAllowedErrorDist. If your solution does solve exactly, you will
        // probably want to add a debug assertion to check your solution against it.
        return true;

    }



    // Simple demo of shared info amongst the team
    // You can modify this as necessary for advanced team strategy
    // Tracking teammates is added to get you started
    public class TeamShare
    {
        public MinionScript[] TeamMates { get; private set; }
        public int TeamSize { get; private set; }
        int currTeamMateRegSpot = 0;

        public TeamShare(int teamSize)
        {
            TeamSize = teamSize;
            TeamMates = new MinionScript[TeamSize];
        }

        public void AddTeamMember(MinionScript m)
        {
            TeamMates[currTeamMateRegSpot] = m;
            ++currTeamMateRegSpot;
        }

        public bool TeamMemberCanBeRescued(out MinionScript firstHelplessMinion)
        {
            firstHelplessMinion = null;

            foreach (var m in TeamMates)
            {
                if (m == null)
                    continue;

                if (m.CanBeRescued)
                {
                    firstHelplessMinion = m;
                    return true;
                }
            }
            return false;
        }
    }

    // Create a base class for our states to have access to the parent MinionBasicDemoStateMachine, and other info
    // This class can be modified!
    abstract class MinionStateBase
    {
        public virtual string Name => throw new System.NotImplementedException();

        protected IFiniteStateMachine<MinionFSMData> ParentFSM;
        protected MinionBasicDemoStateMachine MinionFSM;
        protected MinionScript Minion;
        protected PrisonDodgeballManager Mgr;
        protected PrisonDodgeballManager.Team Team;
        protected TeamShare TeamData;
        protected PrisonDodgeballManager.DodgeballInfo[] dbInfo;

        public virtual void Init(IFiniteStateMachine<MinionFSMData> parentFSM,
            MinionFSMData minFSMData)
        {
            ParentFSM = parentFSM;
            MinionFSM = minFSMData.MinionFSM;
            Minion = minFSMData.Minion;
            Mgr = minFSMData.Mgr;
            Team = minFSMData.Team;
            TeamData = minFSMData.TeamData;
        }

        // Note: You can add extra methods here that you want to be available to all states

        // determineRegion is an expensive operation to determine whether the minion
        // can go to the dodgeball. Don't ask for it if you don't need it
        protected bool UpdateAllDodgeballInfo(bool determineRegion)
        {
            if (dbInfo == null || dbInfo.Length != Mgr.totalBalls)
                dbInfo = new PrisonDodgeballManager.DodgeballInfo[Mgr.totalBalls];

            return Mgr.GetAllDodgeballInfo(Minion.Team, ref dbInfo, determineRegion);
        }

        protected bool FindClosestAvailableDodgeball(out PrisonDodgeballManager.DodgeballInfo dodgeballInfo)
        {

            var dist = float.MaxValue;
            bool found = false;

            dodgeballInfo = default;

            foreach (var db in dbInfo)
            {
                if (!db.IsHeld && db.State == PrisonDodgeballManager.DodgeballState.Neutral && db.Reachable)
                {
                    var d = Vector3.Distance(db.Pos, Minion.transform.position);

                    if (d < dist)
                    {
                        found = true;
                        dist = d;
                        dodgeballInfo = db;
                    }

                }
            }

            return found;
        }

        protected void InternalEnter()
        {
            MinionFSM.Minion.DisplayText(Name);
        }

        // globalTransition parameter is to notify if transition was triggered
        // by a global transition (wildcard)
        public virtual void Exit(bool globalTransition) { }
        public virtual void Exit() { Exit(false); }

        public virtual DeferredStateTransitionBase<MinionFSMData> Update()
        {
            return null;
        }

    }

    // Create a base class for our states to have access to the parent MinionBasicDemoStateMachine, and other info
    abstract class MinionState : MinionStateBase, IState<MinionFSMData>
    {
        public virtual void Enter() { InternalEnter(); }
    }

    // Create a base class for our states to have access to the parent MinionBasicDemoStateMachine, and other info
    abstract class MinionState<S0> : MinionStateBase, IState<MinionFSMData, S0>
    {
        public virtual void Enter(S0 s) { InternalEnter(); }
    }

    // Create a base class for our states to have access to the parent MinionBasicDemoStateMachine, and other info
    abstract class MinionState<S0, S1> : MinionStateBase, IState<MinionFSMData, S0, S1>
    {
        public virtual void Enter(S0 s0, S1 s1) { InternalEnter(); }
    }

    // If you need MinionState<>s with more parameters (up to four total), you can add them following the pattern above

    // Go get a ball!
    class CollectBallState : MinionState
    {
        public override string Name => CollectBallStateName;

        bool hasDestBall = false;
        PrisonDodgeballManager.DodgeballInfo destBall;

        DeferredStateTransition<MinionFSMData> GoToThrowSpotTransition;
        DeferredStateTransition<MinionFSMData> DefenseDemoTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            GoToThrowSpotTransition = ParentFSM.CreateStateTransition(GoToThrowSpotStateName);
            DefenseDemoTransition = ParentFSM.CreateStateTransition(DefensiveDemoStateName);
        }

        public override void Enter()
        {
            base.Enter();

            UpdateAllDodgeballInfo(true);

            if (FindClosestAvailableDodgeball(out destBall))
            {
                hasDestBall = true;
                Minion.GoTo(destBall.Pos);
            }

        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // could pick up a ball accidentally before getting to desired ball
            if (Minion.HasBall)
                return GoToThrowSpotTransition;

            //update our info about the destination ball (and other balls)
            UpdateAllDodgeballInfo(true);
            if (hasDestBall)
            {
                destBall = this.dbInfo[destBall.Index];

                if (destBall.IsHeld || destBall.State != PrisonDodgeballManager.DodgeballState.Neutral || !destBall.Reachable)
                {
                    hasDestBall = false;
                }

            }

            if (!hasDestBall)
            {
                if (FindClosestAvailableDodgeball(out destBall))
                {
                    hasDestBall = true;

                }
            }

            if (hasDestBall)
            {
                // The ball might be moving, so keep updating. GoTo() is smart enough
                // to not keep performing full A* if it doesn't need to, so safe to call often.
                Minion.GoTo(destBall.NavMeshPos);
            }
            else
            {
                // No ball, so focus on defense
                ret = DefenseDemoTransition;
            }

            return ret;
        }
    }


    // This state gets the minion close to the enemy for a throw (or a rescue of a buddy)
    class GoToThrowSpotState : MinionState
    {
        public override string Name => GoToThrowSpotStateName;

        DeferredStateTransition<MinionFSMData> CollectBallTransition;
        DeferredStateTransition<MinionFSMData, MinionScript> RescueTransition;
        DeferredStateTransition<MinionFSMData> ThrowBallTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
            RescueTransition = ParentFSM.CreateStateTransition<MinionScript>(RescueStateName, null, true);
            ThrowBallTransition = ParentFSM.CreateStateTransition(ThrowBallStateName);
        }

        public override void Enter()
        {
            base.Enter();

            Minion.GoTo(Mgr.TeamAdvance(Team).position);
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // just in case something bad happened
            if (!Minion.HasBall)
            {
                return CollectBallTransition;
            }

            if (Minion.ReachedTarget())
            {
                if (TeamData != null && TeamData.TeamMemberCanBeRescued(out var m))
                {
                    RescueTransition.Arg0 = m;
                    ret = RescueTransition;
                }
                else
                    ret = ThrowBallTransition;
            }

            return ret;
        }
    }


    // Rescue a buddy
    class RescueState : MinionState<MinionScript>
    {
        public override string Name => RescueStateName;

        MinionScript buddy;

        DeferredStateTransition<MinionFSMData> CollectBallTransition;
        DeferredStateTransition<MinionFSMData> ThrowBallTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
            ThrowBallTransition = ParentFSM.CreateStateTransition(ThrowBallStateName);
        }

        public override void Enter(MinionScript m)
        {
            base.Enter(m);

            buddy = m;

            Minion.FaceTowards(buddy.transform.position);

            //if (TeamData.TeamMemberCanBeRescued(out buddy))
            //{
            //    Minion.FaceTowards(buddy.transform.position);
            //}

        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // just in case something bad happened
            if (!Minion.HasBall)
            {
                return CollectBallTransition;
            }

            if (buddy != null && buddy.CanBeRescued)
            {
                //Minion.FaceTowards(buddy.transform.position);
            }
            else
            {
                if (TeamData != null && TeamData.TeamMemberCanBeRescued(out buddy))
                {
                    //Minion.FaceTowards(buddy.transform.position);
                }
                else
                    buddy = null;

            }

            // Nothing to do without buddy in prison...
            if (buddy == null)
                return ThrowBallTransition; // we should have a ball still...

            var canThrow = PredictThrow(Minion.HeldBallPosition, Minion.ThrowSpeed, Physics.gravity, buddy.transform.position,
                    buddy.Velocity, buddy.transform.forward, MaxAllowedThrowPositionError,
                    out var univVDir, out var speedScalar, out var interceptT, out var altT);

            if (canThrow)
            {
                var intercept = Minion.HeldBallPosition + univVDir * speedScalar * interceptT;

                var angle = Minion.AbsAngleWith(intercept);

                if (angle < Minion.MaxAllowedOffAngleThrow)
                {
                    var speedNorm = speedScalar / Minion.ThrowSpeed;

                    if (Minion.ThrowBall(univVDir, speedNorm))
                        ret = CollectBallTransition;
                }
                else
                {
                    //Minion.FaceTowards(intercept);

                    // note this is determined from minion prefab and xz plane distance of hold spot from minion center (-0.622f)
                    // must be orthogonal (placed to the side of center)
                    var holdOffsetDist = -0.622f;

                    var newTargPos = FindTurnTowardsTargetForAim(intercept, Minion.transform.position, Minion.transform.forward, holdOffsetDist);
                    Minion.FaceTowards(newTargPos);

                }
            }

            return ret;
        }
    }


    // Throw the ball at the enemy
    class ThrowBallState : MinionState
    {
        public override string Name => ThrowBallStateName;

        int opponentIndex = -1;
        PrisonDodgeballManager.OpponentInfo opponentInfo;
        bool hasOpponent = false;

        DeferredStateTransition<MinionFSMData> CollectBallTransition;
        DeferredStateTransition<MinionFSMData> DefenseDemoTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
            DefenseDemoTransition = ParentFSM.CreateStateTransition(DefensiveDemoStateName);
        }


        public override void Enter()
        {
            base.Enter();


            if (Mgr.FindClosestNonPrisonerOpponentIndex(Minion.transform.position, Team, out opponentIndex))
            {
                if (hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo))
                {
                    Minion.FaceTowards(opponentInfo.Pos);
                }
            }
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // just in case something bad happened
            if (!Minion.HasBall)
            {
                return CollectBallTransition;
            }

            // Check if opponent still valid
            if ((hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo)) &&
                !opponentInfo.IsPrisoner && !opponentInfo.IsFreedPrisoner)
            {
                Minion.FaceTowards(opponentInfo.Pos);
            }
            else
            {
                if (Mgr.FindClosestNonPrisonerOpponentIndex(Minion.transform.position, Team, out opponentIndex))
                {
                    if (hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo))
                    {
                        Minion.FaceTowards(opponentInfo.Pos);
                    }
                }
            }

            // Nothing to do without opponent...
            if (!hasOpponent)
                return DefenseDemoTransition;



            var canThrow = PredictThrow(Minion.HeldBallPosition, Minion.ThrowSpeed, Physics.gravity,
                    opponentInfo.Pos, opponentInfo.Vel, opponentInfo.Forward, MaxAllowedThrowPositionError,
                    out var univVDir, out var speedScalar, out var interceptT, out var altT);

            if (canThrow)
            {
                var intercept = Minion.HeldBallPosition + univVDir * speedScalar * interceptT;

                var angle = Minion.AbsAngleWith(intercept);

                if (angle < Minion.MaxAllowedOffAngleThrow)
                {
                    var speedNorm = speedScalar / Minion.ThrowSpeed;

                    if (Minion.ThrowBall(univVDir, speedNorm))
                        ret = CollectBallTransition;
                }
                else
                {
                    // note this is determined from minion prefab and xz plane distance of hold spot from minion center (-0.622f)
                    // must be orthogonal (placed to the side of center)
                    var holdOffsetDist = -0.622f;

                    var newTargPos = FindTurnTowardsTargetForAim(intercept, Minion.transform.position, Minion.transform.forward, holdOffsetDist);
                    Minion.FaceTowards(newTargPos);

                    // The following useful for visualizing aim towards target

                    //var targetFacingDir = newTargPos - Minion.transform.position;

                    //var minionToNewIntercept2d = new Vector2(targetFacingDir.x, targetFacingDir.z);


                    //var newHoldPos = Minion.transform.position + Vector3.Cross(Vector3.up, targetFacingDir).normalized * holdOffsetDist;

                    //var holdToIntercept2d = new Vector2(intercept.x - newHoldPos.x, intercept.z - newHoldPos.z);

                    //Debug.DrawLine(newHoldPos, intercept, Color.magenta);
                    //Debug.DrawLine(Minion.transform.position, newTargPos, Color.white);
                    //Debug.DrawRay(Minion.transform.position, Minion.transform.forward, Color.green);



                    //var checkAngle = Vector2.Angle(holdToIntercept2d, minionToNewIntercept2d);

                    //if(checkAngle > 1f) 
                    //    Debug.LogWarning($"Expected parallel: {checkAngle}");

                }
            }

            return ret;
        }
    }


    // A not very effective defensive strategy. Mainly a demonstration of calling
    // Minion.Evade()
    class DefensiveDemoState : MinionState
    {
        public override string Name => DefensiveDemoStateName;

        float lastEvade;
        float evadeWaitTimeSec;
        bool doPause = false;
        float pauseStart;
        float pauseDuration;

        DeferredStateTransition<MinionFSMData> GoToThrowSpotTransition;
        DeferredStateTransition<MinionFSMData> CollectBallTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            GoToThrowSpotTransition = ParentFSM.CreateStateTransition(GoToThrowSpotStateName);
            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
        }


        protected bool RandomGoTo()
        {
            var r = Minion.GoTo(Mgr.TeamHome(Team).position + 6f * (new Vector3(Random.value, 0f, Random.value)));

            if (!r)
                Debug.LogError("Could not GOTO in DefenseDemoState");

            return r;
        }



        public override void Enter()
        {
            base.Enter();

            RandomGoTo();

            lastEvade = Time.timeSinceLevelLoad;

            evadeWaitTimeSec = 2f * Minion.EvadeCoolDownTimeSec + 0.1f;
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            if (Minion.HasBall)
                return GoToThrowSpotTransition;

            PrisonDodgeballManager.DodgeballInfo ball;

            UpdateAllDodgeballInfo(true);

            if (FindClosestAvailableDodgeball(out ball))
            {
                return CollectBallTransition;
            }

            if (!doPause && Minion.ReachedTarget())
            {
                pauseStart = Time.timeSinceLevelLoad;
                doPause = true;
                pauseDuration = Random.value * 3f;
            }

            if (doPause)
            {
                Minion.FaceTowards(Mgr.TeamPrison(Team).position);

                if (Time.timeSinceLevelLoad - pauseStart >= pauseDuration)
                {
                    doPause = false;
                    RandomGoTo();
                }
            }
            else if (Time.timeSinceLevelLoad - lastEvade >= evadeWaitTimeSec)
            {

                lastEvade = Time.timeSinceLevelLoad;

                var r = Random.Range(0, 3);

                MinionScript.EvasionDirection ev;

                switch (r)
                {
                    case 0:
                        ev = MinionScript.EvasionDirection.Brake;
                        break;
                    case 1:
                        ev = MinionScript.EvasionDirection.Left;
                        break;
                    case 2:
                        ev = MinionScript.EvasionDirection.Right;
                        break;
                    default:
                        ev = MinionScript.EvasionDirection.Brake;
                        break;
                }

                Minion.Evade(ev, Random.Range(0.6f, 1.0f));
            }


            return ret;
        }
    }


    // Go directly to jail. Do not pass go. Do not collect $200 
    class GoToPrisonState : MinionState
    {
        public override string Name => GoToPrisonStateName;

        int waypointIndex = 0;

        DeferredStateTransition<MinionFSMData> LeavePrisonTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            LeavePrisonTransition = ParentFSM.CreateStateTransition(LeavePrisonStateName);
        }


        public override void Enter()
        {
            base.Enter();

            waypointIndex = 0;

            Minion.GoTo(Mgr.TeamGutterEntranceLeft(Team).position);
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            if (!Minion.IsPrisoner)
            {
                return LeavePrisonTransition;
                //if (Minion.HasBall)
                //    return GoToThrowSpotBallStateName;
                //else
                //    return GoHomeStateName;
            }

            if (Minion.ReachedTarget())
            {
                if (waypointIndex == 0)
                {
                    ++waypointIndex;
                    Minion.GoTo(Mgr.TeamGutterEndLeft(Team).position);
                }
                else if (waypointIndex == 1)
                {
                    ++waypointIndex;
                    Minion.GoTo(Mgr.TeamPrison(Team).position);
                }
                else
                {
                    Minion.FaceTowards(Mgr.TeamHome(Team).position);
                }
            }

            return ret;
        }
    }

    // Free! 
    class LeavePrisonState : MinionState
    {
        public override string Name => LeavePrisonStateName;

        int waypointIndex = 0;

        DeferredStateTransition<MinionFSMData> GoToThrowSpotTransition;
        DeferredStateTransition<MinionFSMData> GoHomeTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            GoToThrowSpotTransition = ParentFSM.CreateStateTransition(GoToThrowSpotStateName);
            GoHomeTransition = ParentFSM.CreateStateTransition(GoHomeStateName);
        }


        public override void Enter()
        {
            base.Enter();

            waypointIndex = 0;

            Minion.GoTo(Mgr.TeamGutterEndRight(Team).position);
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            if (Minion.ReachedTarget())
            {
                if (waypointIndex == 0)
                {
                    ++waypointIndex;
                    Minion.GoTo(Mgr.TeamGutterEntranceRight(Team).position);
                }
                else
                {
                    if (Minion.HasBall)
                        return GoToThrowSpotTransition;
                    else
                        return GoHomeTransition;

                }
            }

            return ret;
        }
    }


    // Going home. Maybe after a jailbreak
    class GoHomeState : MinionState
    {
        public override string Name => GoHomeStateName;

        DeferredStateTransition<MinionFSMData> CollectBallTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
        }


        public override void Enter()
        {
            base.Enter();

            if (!Minion.GoTo(Mgr.TeamHome(Team).position))
            {
                Debug.LogError($"Could not find a way home! NavMesh Mask: {Minion.NavMeshMaskToString()}");
            }
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            if (Minion.ReachedTarget())
            {
                ret = CollectBallTransition;
            }

            return ret;
        }
    }


    class RestState : MinionState
    {
        public override string Name => RestStateName;

        public override void Enter()
        {
            base.Enter();

            if (!Minion.GoTo(Mgr.TeamHome(Team).position))
            {
                Debug.LogError($"Could not find a way home! NavMesh Mask: {Minion.NavMeshMaskToString()}");
            }
        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            return ret;
        }
    }


    // This is a special state that never exits. It coexists with the current state.
    // It's always evaluated first. It's only job is supposed to identify global/wildcard
    // transitions (it shouldn't do anything that modifies anything externally other than
    // return a desired transition).
    class GlobalTransitionState : MinionState
    {
        public override string Name => GlobalTransitionStateName;

        bool wasPrisioner = false;

        DeferredStateTransition<MinionFSMData> RestTransition;
        DeferredStateTransition<MinionFSMData> PrisonTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            RestTransition = ParentFSM.CreateStateTransition(RestStateName);
            PrisonTransition = ParentFSM.CreateStateTransition(GoToPrisonStateName);
        }


        public override void Enter()
        {
            base.Enter();
        }

        // The global state never exits
        //public override void Exit(bool globalTransition)
        //{
        //}

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            //Debug.Log($"Mgr: {Mgr == null} MinionFSM: {MinionFSM == null} ");

            if (Mgr.IsGameOver && !ParentFSM.CurrentState.Name.Equals(RestStateName))
            {
                ret = RestTransition;
            }
            else if (Minion.IsPrisoner && !wasPrisioner)
            {
                // Just switched to prisoner! Uh oh. Gotta head to prison. :-(
                ret = PrisonTransition;

                wasPrisioner = true;
            }
            else if (!Minion.IsPrisoner && wasPrisioner)
            {
                wasPrisioner = false;
            }

            return ret;
        }
    }


    private void Awake()
    {
        Minion = GetComponent<MinionScript>();

        if (Minion == null)
            Debug.LogError("No minion script");
    }


    protected void InitTeamData()
    {
        Mgr.SetTeamText(Minion.Team, StudentName);

        var o = Mgr.GetTeamDataShare(Minion.Team);

        if (o == null)
        {
            //Debug.Log($"Team Size: {Mgr.TeamSize}");
            TeamData = new TeamShare(Mgr.TeamSize);
            Mgr.SetTeamDataShare(Minion.Team, TeamData);
        }
        else
        {
            TeamData = o as TeamShare;

            if (TeamData == null)
                Debug.LogError("TeamData is null!");
        }

        TeamData.AddTeamMember(Minion);
    }


    // Start is called before the first frame update
    protected void Start()
    {

        Mgr = PrisonDodgeballManager.Instance;

        InitTeamData();

        var minionFSMData = new MinionFSMData(this, Minion, Mgr, Minion.Team, TeamData);

        fsm = new FiniteStateMachine<MinionFSMData>(minionFSMData);

        // Handles global/wildcard transitions. This state is a co-state that
        // never exits. Triggered transitions only change the current state.
        // The global state should only handle initiating transitions
        fsm.SetGlobalTransitionState(new GlobalTransitionState());

        fsm.AddState(new CollectBallState(), true);
        fsm.AddState(new GoToThrowSpotState());
        fsm.AddState(new ThrowBallState());
        fsm.AddState(new DefensiveDemoState());
        fsm.AddState(new GoToPrisonState());
        fsm.AddState(new LeavePrisonState());
        fsm.AddState(new GoHomeState());
        fsm.AddState(new RescueState());
        fsm.AddState(new RestState());

        //MinionBasicDemoStateMachine, GameAIStudentWork, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
        //Debug.Log(this.GetType().AssemblyQualifiedName);

    }

    protected void Update()
    {
        fsm.Update();

        // For debugging, could repurpose the DisplayText of the Minion.
        // To do so affecting all states, implement the FSM's Update like so:
        //Minion.DisplayText(Minion.NavMeshCurrentSurfaceToString());

    }

}
