namespace csharp Scaling.Data

typedef string InstanceId

enum Operation {	
	None,  // the omnipresent null value
	Add
	Subtract,
	Multiply,
	Divide	
}

const Operation FirstOperation = Operation.None;
const Operation LastOperation = Operation.Divide;


union InputData {
	// used for Operation.Add through Operation.Divide
	1 : double SecondOperand   
}


struct InstanceDescriptor {
	1 : InstanceId	Id
}

struct Workpack {
	1 : InstanceDescriptor	Instance
	2 : Operation  			OpCode
	3 : InputData  			Input
}

struct InstanceState {
	1 : InstanceId	Id
	2 : i64 		Revision
	3 : double		LastResult
}


// EOF
