﻿using NSpec;
using Entitas;

class describe_ReactiveSystem : nspec {

    void when_created() {
        Pool pool = null;
        ReactiveSystem reactiveSystem = null;
        ReactiveSubSystemSpy subSystem = null;
        MultiReactiveSubSystemSpy multiSubSystem = null;
        before = () => {
            pool = new Pool(CID.NumComponents);
        };

        context["OnEntityAdded"] = () => {
            before = () => {
                subSystem = getSubSystemSypWithOnEntityAdded();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
            };

            it["does not execute its subsystem when no entities were collected"] = () => {
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
            };

            it["executes when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes only once when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["doesn't execute when triggeringMatcher doesn't match"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };

            it["deactivates"] = () => {
                reactiveSystem.Deactivate();
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };

            it["activates"] = () => {
                reactiveSystem.Deactivate();
                reactiveSystem.Activate();
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };
        };

        context["OnEntityRemoved"] = () => {
            before = () => {
                subSystem = getSubSystemSypWithOnEntityRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
            };

            it["executes when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.RemoveComponentA();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes only once when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.RemoveComponentA();
                reactiveSystem.Execute();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["doesn't execute when triggeringMatcher doesn't match"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.AddComponentC();
                e.RemoveComponentC();
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };
        };

        context["OnEntityAddedOrRemoved"] = () => {
            it["executes when added"] = () => {
                subSystem = getSubSystemSypWithOnEntityAddedOrRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes when removed"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                subSystem = getSubSystemSypWithOnEntityAddedOrRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
                e.RemoveComponentA();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["collects matching entities created or modified in the subsystem"] = () => {
                var sys = new EntityEmittingSubSystem(pool);
                reactiveSystem = new ReactiveSystem(pool, sys);
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();
                sys.entites.Length.should_be(1);
                reactiveSystem.Execute();
                sys.entites.Length.should_be(1);
            };
        };

        context["MultiReactiveSystem"] = () => {
            before = () => {
                var matchers = new IMatcher[] {
                    Matcher.AllOf(new [] { CID.ComponentA }),
                    Matcher.AllOf(new [] { CID.ComponentB })
                };
                var eventTypes = new [] {
                    GroupEventType.OnEntityAdded,
                    GroupEventType.OnEntityRemoved,
                };
                multiSubSystem = new MultiReactiveSubSystemSpy(matchers, eventTypes);
                reactiveSystem = new ReactiveSystem(pool, multiSubSystem);
            };
            
            it["executes when a triggering matcher matches"] = () => {
                var eA = pool.CreateEntity();
                eA.AddComponentA();
                var eB = pool.CreateEntity();
                eB.AddComponentB();
                reactiveSystem.Execute();

                multiSubSystem.didExecute.should_be(1);
                multiSubSystem.entities.Length.should_be(1);
                multiSubSystem.entities.should_contain(eA);

                eB.RemoveComponentB();
                reactiveSystem.Execute();

                multiSubSystem.didExecute.should_be(2);
                multiSubSystem.entities.Length.should_be(1);
                multiSubSystem.entities.should_contain(eB);
            };
        };
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityAdded() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityAdded);
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityRemoved() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityRemoved);
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityAddedOrRemoved() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityAddedOrRemoved);
    }

    IMatcher allOfAB() {
        return Matcher.AllOf(new [] {
            CID.ComponentA,
            CID.ComponentB
        });
    }
}

