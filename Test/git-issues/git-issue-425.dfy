// RUN: %dafny /compile:0 "%s" > "%t"
// RUN: %dafny /noVerify /compile:4 /compileTarget:cs "%s" >> "%t"
// RUN: %dafny /noVerify /compile:4 /compileTarget:js "%s" >> "%t"
// RUN: %dafny /noVerify /compile:4 /compileTarget:go "%s" >> "%t"
// RUN: %dafny /noVerify /compile:4 /compileTarget:java "%s" >> "%t"
// RUN: %diff "%s.expect" "%t"

method M() returns (x: int, ghost y: int) {
  return 42, 43;
}

datatype Color = Red | Blue
  
method Main() {
  var x0, y0 := M();  // this is fine: x0 is compiled, y0 is ghost
  print x0, "\n";
  var c := Red;
  match c
  case Red =>
    var x1, y1 := M();  // this used to generate an error, saying y1 is not ghost :(
    print x1, "\n";
  case Blue =>
}
