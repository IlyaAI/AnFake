grammar Pipeline;

/*
 * Parser Rules
 */

pipeline: step;

step: 
	unaryStep				# InternalUnaryStep
	| step '=>' unaryStep	# InternalParallelStep
	| step '->' unaryStep	# InternalSequentialStep
	;

unaryStep:
	buildRun
	| optionalBuildRun
	| '(' step ')'
	;

optionalBuildRun:
	'[' buildRun ']'
	;

buildRun:
	buildRunName											# InternalBuildRunVoid
	| buildRunName '(' buildRunParams ')'					# InternalBuildRunIn
	| buildRunName 'as' Identifier							# InternalBuildRunOut
	| buildRunName '(' buildRunParams ')' 'as' Identifier	# InternalBuildRunInOut
	;

buildRunName:
	Identifier 
	| QuotedIdentifier
	;

buildRunParams:
	Identifier (',' QuotedIdentifier)*	
	;

/*
 * Lexer Rules
 */

Identifier: [A-Za-z_][0-9A-Za-z_]*;

QuotedIdentifier: ['][0-9A-Za-z_\.\-:@#?&=+% \\/]+['];

Spaces:	[ \t\r\n]+ -> skip;
