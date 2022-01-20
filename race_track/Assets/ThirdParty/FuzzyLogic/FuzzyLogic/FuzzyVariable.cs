﻿using System;
using Tochas.FuzzyLogic.Expressions;

namespace Tochas.FuzzyLogic
{
    public class FuzzyVariable<T> where T : struct, IConvertible
    {
        public T LinguisticVariable { get; private set; }
        public IMembershipFunction MembershipFunction { get; private set; }

        public FuzzyVariable(T linguisticVariable, IMembershipFunction membershipFunction)
        {
            FuzzyUtils.IsGenericParameterValid<T>();
            this.LinguisticVariable = linguisticVariable;
            this.MembershipFunction = membershipFunction;
        }

        public FuzzyValue<T> fX(float x)
        {
            float y = this.MembershipFunction.fX(x);
            return new FuzzyValue<T>(this.LinguisticVariable, y);
        }

        public IFuzzyExpression AsExpression()
        {
            return new FuzzyVariableExpression<T>(this.LinguisticVariable);
        }

        public IFuzzyExpression Expr()
        {
            return this.AsExpression();
        }

        public static implicit operator FuzzyVariableExpression<T>(FuzzyVariable<T> self)
        {
            return new FuzzyVariableExpression<T>(self.LinguisticVariable);
        }
    }
}
