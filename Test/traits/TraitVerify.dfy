// RUN: %dafny /print:"%t.print" /dprint:"%t.dprint" "%s" > "%t"
// RUN: %diff "%s.expect" "%t"

trait Tr<X> { }

class A extends Tr<int> { }
class B extends Tr<real> { }
class C<Y> extends Tr<Y> { }

method M(a: A, b: B) {
  var c: C?;
  var t;  // Tr?<int>
  t := a;
  t := c;
}

method N(a: A, b: B) {
  var c: C?<int>;
  var t: Tr<int>;
  t := a;
  t := c;  // error: c may be null
}

method Q(a: A?) returns (t: Tr<int>) {
  t := a;  // error: a may be null
}

method P(a: A?) {
  var t: A;
  t := a;  // error: a may be null
}

method R(a: A) {
  var t: A?;
  t := a;
}

// -----------------------------------------------------

module TestParentTraitRelation {
  trait BTrait {
    ghost var Repr: set<object>
    predicate Valid()
      reads this, Repr
      ensures Valid() ==> this in Repr
    method SomeMethod()
      requires Valid()
      modifies Repr
      ensures Valid() && fresh(Repr - old(Repr))
  }

  trait CTrait {
    ghost var Repr: set<object>
    predicate Valid()
      reads this, Repr
      ensures Valid() ==> this in Repr
  }

  method doSomething(b: BTrait, c: BTrait)
    requires b.Valid() && c.Valid() && b.Repr !! c.Repr
    modifies b.Repr
    ensures b.Valid() && c.Valid()
  {
    b.SomeMethod();
  }
}

// -----------------------------------------------------

module TraitsExtendingTraits {
  trait A<Y0, Y1> {
    var y0: Y0
    const y1: Y1
  }

  trait K<Y> extends A<Y, int> {
    const k := y1
  }

  class G<X> extends K<X> {
    constructor () {
    }  // error: must assign to y0 (but assigning to y1 is not necessary)
  }

  // ----

  trait B {
    method Get() returns (x: int)
  }
  trait M extends B {
    method Get() returns (x: int)
      ensures 0 <= x <= 20
    {
      return 15;
    }
  }
  trait N extends B {
  }
  class H extends M, N {
    constructor () { }
  }
  method BMNH() {
    var h := new H();
    var b: B, m: M, n: N := h, h, h;

    if
    case true =>
      var x := h.Get();
      assert x <= 20;
    case true =>
      var x := b.Get();
      assert x <= 20;  // error: this is not provable
    case true =>
      var x := m.Get();
      assert x <= 20;
    case true =>
      var x := n.Get();
      assert x <= 20;  // error: this is not provable
  }
}

// -----

module StandardIdiom {
  trait ValidReprIdiom {
    ghost var Repr: set<object>

    predicate Valid()
      reads this, Repr
      ensures Valid() ==> this in Repr
  }

  class Filter extends ValidReprIdiom {
    const next: ValidReprIdiom

    predicate Valid()
      reads this, Repr
      ensures Valid() ==> this in Repr
    {
      this in Repr &&
      next in Repr && next.Repr <= Repr && this !in next.Repr && next.Valid()
    }

    constructor(next: ValidReprIdiom) {
      this.next := next;
    }
  }

  class ABC extends ValidReprIdiom {
    predicate Valid()
      reads this, Repr
      ensures Valid() ==> this in Repr
    {
      this in Repr
    }

    constructor()
      ensures Valid() && fresh(Repr)
    {
      Repr := {this};
    }
  }
}

module OverrideWithDifferentFuels {
  trait Tr {
    function method F(x: nat): nat
  }
  class Cl extends Tr {
    function method F(x: nat): nat {
      if x == 0 then 0 else F(x - 1)
    }
  }
  method Test(c: Cl) {
    var t: Tr := c;
    var x := t.F(5);
    var y := c.F(5);
    assert x == y;
  }
}
