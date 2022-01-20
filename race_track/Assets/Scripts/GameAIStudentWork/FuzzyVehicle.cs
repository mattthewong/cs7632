using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameAI;

// All the Fuzz
using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;

using FuzzyBinary = System.Func<Tochas.FuzzyLogic.IFuzzyExpression, Tochas.FuzzyLogic.IFuzzyExpression, Tochas.FuzzyLogic.IFuzzyExpression>;
using FuzzyUnary = System.Func<Tochas.FuzzyLogic.IFuzzyExpression, Tochas.FuzzyLogic.IFuzzyExpression>;

namespace GameAICourse
{

    public class FuzzyVehicle : AIVehicle
    {

        // TODO create some Fuzzy Set enumeration types, and member variables for:
        // Fuzzy Sets (input and output), one or more Fuzzy Value Sets, and Fuzzy
        // Rule Sets for each output.
        // Also, create some methods to instantiate each of the member variables
        // member vars
        float lastDistTraveled = 0f;
        float lastDistSampleTime = 0f;
        float signedAngle = 0f;
        float speed = 0f;
        float speedNormed = 0f;
        Vector3 centerlineDist = new Vector3 { };
        // define enums
        enum FzFixed { Fixed }
        enum FzStuckThrottle { Stuck }
        enum FzBreakThrottle { Brake } 
        enum FzVarThrottle { Low, Med, High, LightBrake, SharpBrake }
        enum FzSteering { Left, Center, Right, HarshLeft, HarshRight }
        enum FzRoadSide { Left, Right }

        // set fuzzy sets
        FuzzySet<FzSteering> fzSteeringSet;
        FuzzySet<FzFixed> fzFixedSet;
        FuzzySet<FzVarThrottle> fzVarThrottleSet;
        FuzzySet<FzRoadSide> fzRoadSideSet;
        // set fuzzy rules sets
        FuzzyRuleSet<FzSteering> fzSteeringRuleSet;
        FuzzyRuleSet<FzStuckThrottle> fzStuckThrottleRuleSet;
        FuzzyRuleSet<FzVarThrottle> fzVarThrottleRuleSet;
        FuzzyRuleSet<FzRoadSide> fzRoadSideRuleSet;
        // set shared fuzzy value set
        FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

        private FuzzySet<FzFixed> GetFixedSet()
        {
            IMembershipFunction fixedFx = new TrapezoidMembershipFunction(
              new Coords(-1001, 1f),
              new Coords(-1000, 1f),
              new Coords(1000, 1f),
              new Coords(1001, 1f));

            FuzzySet<FzFixed> set = new FuzzySet<FzFixed>();

            set.Set(FzFixed.Fixed, fixedFx);

            return set;
        }

        private FuzzySet<FzVarThrottle> GetVarThrottleSet()
        {
            IMembershipFunction lowThrottleFx = new TrapezoidMembershipFunction(
                    new Coords(-1, 0.1f),
                    new Coords(0.1f, 1f),
                    new Coords(0.15f, 1f),
                    new Coords(0.2f, 0.25f));

            IMembershipFunction medThrottleFx = new TrapezoidMembershipFunction(
                  new Coords(0.2f, 0.25f),
                  new Coords(0.25f, .25f),
                  new Coords(0.3f, 1f),
                  new Coords(0.4f, 0f));

            IMembershipFunction highThrottleFx = new TrapezoidMembershipFunction(
                      new Coords(0.9f, 0.1f),
                      new Coords(1.1f, 0.3f),
                      new Coords(1.2f, 0.5f),
                      new Coords(1.9f, 1f));

            var sharpBrakeThrottlePos = -0.75f; // Hard brake throttle pos
            var lightBrakeThrottlePos = -0.25f; // Hard brake throttle pos


            IMembershipFunction sharpBrakeFx = new TriangularMembershipFunction(
            new Coords(sharpBrakeThrottlePos - 1f, 0f),
            new Coords(sharpBrakeThrottlePos, 1f),
            new Coords(sharpBrakeThrottlePos + 1f, 0f));

            IMembershipFunction lightBrakeFx = new TriangularMembershipFunction(
            new Coords(lightBrakeThrottlePos - 1f, 0f),
            new Coords(lightBrakeThrottlePos, 1f),
            new Coords(lightBrakeThrottlePos + 1f, 0f));


            FuzzySet<FzVarThrottle> set = new FuzzySet<FzVarThrottle>();

            set.Set(FzVarThrottle.Low, lowThrottleFx);
            set.Set(FzVarThrottle.Med, medThrottleFx);
            set.Set(FzVarThrottle.High, highThrottleFx);
            set.Set(FzVarThrottle.SharpBrake, sharpBrakeFx);
            set.Set(FzVarThrottle.LightBrake, lightBrakeFx);

            return set;
        }

        private FuzzySet<FzSteering> GetSteeringSet()
        {
            IMembershipFunction leftFx = new TrapezoidMembershipFunction(
                 new Coords(-1f, 1f),
                 new Coords(-.6f, 1f),
                 new Coords(-.4f, 1f),
                 new Coords(-0.16f, 0.1f)
             );

            IMembershipFunction centerFx = new TrapezoidMembershipFunction(
                 new Coords(-0.15f, 0.1f),
                 new Coords(-0.05f, 1f),
                 new Coords(0.05f, 1f),
                 new Coords(0.15f, 0.1f)
            );

            IMembershipFunction rightFx = new TrapezoidMembershipFunction(
                 new Coords(0.16f, 0.1f),
                 new Coords(0.4f, 1f),
                 new Coords(0.6f, 1f),
                 new Coords(1f, 1f)
            );

            IMembershipFunction harshLeftFx = new TrapezoidMembershipFunction(
                  new Coords(-3.8f, 1f),
                  new Coords(-3f, 1f),
                  new Coords(-.5f, 0.1f),
                  new Coords(-0.16f, 0f)
              );

            IMembershipFunction harshRightFx = new TrapezoidMembershipFunction(
                 new Coords(0.16f, 0f),
                 new Coords(0.5f, 0.1f),
                 new Coords(3f, 1f),
                 new Coords(3.8f, 1f)
            );

            FuzzySet<FzSteering> set = new FuzzySet<FzSteering>();

            set.Set(FzSteering.Left, leftFx);
            set.Set(FzSteering.Center, centerFx);
            set.Set(FzSteering.Right, rightFx);
            set.Set(FzSteering.HarshLeft, harshLeftFx);
            set.Set(FzSteering.HarshRight, harshRightFx);

            return set;
        }

        private FuzzySet<FzRoadSide> GetRoadSideSet()
        {
            IMembershipFunction leftEdgeFx = new TrapezoidMembershipFunction(
                 new Coords(-.9f, 1f),
                 new Coords(-.7f, 1f),
                 new Coords(-0.2f, 0f),
                 new Coords(0, 0f)
             );

            IMembershipFunction rightEdgeFx = new TrapezoidMembershipFunction(
                 new Coords(0f, 0f),
                 new Coords(.2f, 0f),
                 new Coords(.7f, 1f),
                 new Coords(.9f, 1f)
            );

            FuzzySet<FzRoadSide> set = new FuzzySet<FzRoadSide>();

            set.Set(FzRoadSide.Left, leftEdgeFx);
            set.Set(FzRoadSide.Right, rightEdgeFx);

            return set;
        }

        private FuzzyRuleSet<FzSteering> GetSteeringRuleSet()
        {
            IMembershipFunction leftFx = new TrapezoidMembershipFunction(
             new Coords(-1f, 1f),
             new Coords(-.6f, 1f),
             new Coords(-.4f, 1f),
             new Coords(-0.16f, 0.1f)
         );

            IMembershipFunction centerFx = new TrapezoidMembershipFunction(
                 new Coords(-0.15f, 0.1f),
                 new Coords(-0.05f, 1f),
                 new Coords(0.05f, 1f),
                 new Coords(0.15f, 0.1f)
            );

            IMembershipFunction rightFx = new TrapezoidMembershipFunction(
                 new Coords(0.16f, 0.1f),
                 new Coords(0.4f, 1f),
                 new Coords(0.6f, 1f),
                 new Coords(1f, 1f)
            );

            IMembershipFunction harshLeftFx = new TrapezoidMembershipFunction(
                  new Coords(-3.8f, 1f),
                  new Coords(-3f, 1f),
                  new Coords(-.5f, 0.1f),
                  new Coords(-0.16f, 0f)
              );

            IMembershipFunction harshRightFx = new TrapezoidMembershipFunction(
                 new Coords(0.16f, 0f),
                 new Coords(0.5f, 0.1f),
                 new Coords(3f, 1f),
                 new Coords(3.8f, 1f)
            );

            FuzzySet<FzSteering> set = new FuzzySet<FzSteering>();

            set.Set(FzSteering.Left, leftFx);
            set.Set(FzSteering.Center, centerFx);
            set.Set(FzSteering.Right, rightFx);
            set.Set(FzSteering.HarshLeft, harshLeftFx);
            set.Set(FzSteering.HarshRight, harshRightFx);


            FuzzyRule<FzSteering>[] rules = new FuzzyRule<FzSteering>[]
            {
                       FzSteering.Left.Expr().Then(FzSteering.Left),
                       FzSteering.Center.Expr().Then(FzSteering.Center),
                       FzSteering.Right.Expr().Then(FzSteering.Right),
                       FzSteering.HarshRight.Expr().Then(FzSteering.HarshRight),
                       FzSteering.HarshLeft.Expr().Then(FzSteering.HarshLeft),
            };

            return new FuzzyRuleSet<FzSteering>(set, rules);

        }

        private FuzzyRuleSet<FzVarThrottle> GetVarThrottleRuleSet()
        {
            IMembershipFunction lowThrottleFx = new TrapezoidMembershipFunction(
                    new Coords(-1, 0.1f),
                    new Coords(0.1f, 1f),
                    new Coords(0.15f, 1f),
                    new Coords(0.2f, 0.25f));

            IMembershipFunction medThrottleFx = new TrapezoidMembershipFunction(
                  new Coords(0.2f, 0.25f),
                  new Coords(0.25f, .25f),
                  new Coords(0.3f, 1f),
                  new Coords(0.4f, 0f));

            IMembershipFunction highThrottleFx = new TrapezoidMembershipFunction(
                      new Coords(0.9f, 0.1f),
                      new Coords(1.1f, 0.3f),
                      new Coords(1.2f, 0.5f),
                      new Coords(1.9f, 1f));

            var sharpBrakeThrottlePos = -0.75f; // Hard brake throttle pos
            var lightBrakeThrottlePos = -0.25f; // Hard brake throttle pos


            IMembershipFunction sharpBrakeFx = new TriangularMembershipFunction(
            new Coords(sharpBrakeThrottlePos - 1f, 0f),
            new Coords(sharpBrakeThrottlePos, 1f),
            new Coords(sharpBrakeThrottlePos + 1f, 0f));

            IMembershipFunction lightBrakeFx = new TriangularMembershipFunction(
            new Coords(lightBrakeThrottlePos - 1f, 0f),
            new Coords(lightBrakeThrottlePos, 1f),
            new Coords(lightBrakeThrottlePos + 1f, 0f));


            FuzzySet<FzVarThrottle> set = new FuzzySet<FzVarThrottle>();

            set.Set(FzVarThrottle.Low, lowThrottleFx);
            set.Set(FzVarThrottle.Med, medThrottleFx);
            set.Set(FzVarThrottle.High, highThrottleFx);
            set.Set(FzVarThrottle.SharpBrake, sharpBrakeFx);
            set.Set(FzVarThrottle.LightBrake, lightBrakeFx);


            FuzzyRule<FzVarThrottle>[] rules = new FuzzyRule<FzVarThrottle>[]
            {          
                       FzSteering.Center.Expr().Then(FzVarThrottle.High), // go fast when steering is straight
                       FzVarThrottle.High.Expr().And(FzSteering.Left.Expr()).Then(FzVarThrottle.SharpBrake), // break at high speeds and right turn
                       FzVarThrottle.High.Expr().And(FzSteering.Right.Expr()).Then(FzVarThrottle.SharpBrake), // break at high speeds and right turn
                       FzVarThrottle.Med.Expr().And(FzSteering.Left.Expr()).Then(FzVarThrottle.SharpBrake), // break at med speeds and right turn
                       FzVarThrottle.Med.Expr().And(FzSteering.Right.Expr()).Then(FzVarThrottle.SharpBrake), // break at med speeds and right turn
                       FzVarThrottle.Low.Expr().Then(FzVarThrottle.High),
                       FzVarThrottle.Med.Expr().Then(FzVarThrottle.Med),
                       //FzRoadSide.Left.Expr().Then(FzVarThrottle.LightBrake), // custom rule for forcing light brake
                       //FzRoadSide.Right.Expr().Then(FzVarThrottle.LightBrake), // custom rule for forcing light brake
                       FzVarThrottle.SharpBrake.Expr().Then(FzVarThrottle.High), // quick accel after break                      
            };

            return new FuzzyRuleSet<FzVarThrottle>(set, rules);

        }

        private FuzzyRuleSet<FzStuckThrottle> GetStuckThrottleRuleSet()
        {

            var stuckThrottlePos = 0.1f; // The throttle value we are stuck on

            IMembershipFunction stuckFx = new TriangularMembershipFunction(
            new Coords(stuckThrottlePos - 1f, 0f),
            new Coords(stuckThrottlePos, 1f),
            new Coords(stuckThrottlePos + 1f, 0f));

            FuzzySet<FzStuckThrottle> stuckThrottleSet = new FuzzySet<FzStuckThrottle>();
            stuckThrottleSet.Set(FzStuckThrottle.Stuck, stuckFx);

            FuzzyRule<FzStuckThrottle>[] rules = new FuzzyRule<FzStuckThrottle>[]
            {
                FzFixed.Fixed.Expr().Then(FzStuckThrottle.Stuck),
            };

            return new FuzzyRuleSet<FzStuckThrottle>(stuckThrottleSet, rules);
        }

        protected override void Awake()
        {
            base.Awake();

            StudentName = "Matthew Wong";

            // Only the AI can control. No humans allowed!
            IsPlayer = false;

        }

        protected override void Start()
        {
            base.Start();

            // TODO: You can initialize a bunch of Fuzzy stuff here
            lastDistTraveled = pathTracker.totalDistanceTravelled;
            lastDistSampleTime = Time.timeSinceLevelLoad;
            fzSteeringRuleSet = this.GetSteeringRuleSet();
            fzStuckThrottleRuleSet = this.GetStuckThrottleRuleSet();
            fzVarThrottleRuleSet = this.GetVarThrottleRuleSet();            

            fzFixedSet = this.GetFixedSet();
            fzSteeringSet = this.GetSteeringSet();
            fzVarThrottleSet = this.GetVarThrottleSet();
            fzRoadSideSet = this.GetRoadSideSet();
        }

        override protected void Update()
        {

            // TODO Do all your Fuzzy stuff here and then
            // pass your fuzzy rule sets to ApplyFuzzyRules()
            // Example:
            // ApplyFuzzyRules<FzThrottle, FzWheel>(fzThrottleRuleSet, 
            // fzWheelRuleSet, fzInputValueSet);
            // 
            // The first set maps to throttle and the second maps to steering
            // Throttle = someValue; //[-1f, 1f] -1 is full brake, 0 is neutral, 1 is full throttle
            // Steering = someValue; // [-1f, 1f] -1 if full left, 0 is neutral, 1 is full right

            // negative is left, positive is right; if either are greater than 10, need correction
            centerlineDist = pathTracker.closestPointOnPath / 5f;
            //Debug.Log("CENTERLINE DIST: " + centerlineDist.x);
            //Debug.Log("closest point" + pathTracker.closestPointOnPath.x);
            //signedAngle = Vector3.SignedAngle(centerlineDist, transform.position, Vector3.up);
            //Debug.Log("signed angle: " + signedAngle);
            //Debug.DrawLine(pathTracker.closestPointOnPath);

            // attempt to steer based on future path points
            float currentDist = pathTracker.distanceTravelled;

            var r = pathTracker.totalDistanceTravelled - lastDistTraveled;
            var dt = Time.timeSinceLevelLoad - lastDistSampleTime;

            lastDistTraveled = pathTracker.totalDistanceTravelled;
            lastDistSampleTime = Time.timeSinceLevelLoad;


            float offsetDist = 1;
            if (r != 0 && dt != 0)
            {
                offsetDist = (20 / (r / dt));
            }

            speed = 1f;
            if ((r / dt) != 0)
            {
                speed = r / dt;
            }
            speedNormed = Math.Abs(speed / 10);
            if (float.IsNaN(speedNormed))
            {
                speedNormed = 0.4f;
            }


            float fp = currentDist - offsetDist;

            //Vector3 futureRoadPos = pathTracker.pathCreator.path.GetPointAtDistance(fp, PathCreation.EndOfPathInstruction.Stop);
            ////Vector3 futureRoadDir = pathTracker.pathCreator.path.GetDirectionAtDistance(fp, PathCreation.EndOfPathInstruction.Stop);
            ////Vector3 centerlineDist = pathTracker.closestPointDirectionOnPath;
            //Vector3 vehiclePos = transform.position;
            //Vector3 centerPoint = pathTracker.closestPointOnPath;

            ////Debug.Log("future point: " + futurePoint);

            ////Debug.Log("VEHICLE POS: " + vehiclePos + " FPOS: " + futureRoadPos);
            ////float sa = Vector3.SignedAngle(futureRoadPos, vehiclePos, Vector3.up);
            ////Debug.Log("signed angle: " + sa);

            float signedAngle = Vector3.SignedAngle(transform.forward, pathTracker.closestPointDirectionOnPath, Vector3.up);
            float saNorm = signedAngle / 10f;
            //Debug.Log("signed angle normed: " + saNorm);

            // for throttle
            //Debug.Log("speed: " + speedNormed);

            fzVarThrottleSet.Evaluate(speedNormed, fzInputValueSet);
            // for steering
            
            fzSteeringSet.Evaluate(saNorm, fzInputValueSet);
            fzRoadSideSet.Evaluate(saNorm, fzInputValueSet);


            ApplyFuzzyRules<FzVarThrottle, FzSteering>(
                    fzVarThrottleRuleSet,
                    fzSteeringRuleSet,
                    fzInputValueSet);
            // recommend you keep the base call at the end, after all your FuzzyVehicle code so that
            // control inputs can be processed properly (Throttle, Steering)
            base.Update();
        }

    }
}
