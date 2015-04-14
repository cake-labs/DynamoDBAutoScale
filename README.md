# DynamoDBAutoScale
DynamoDBAutoScale is a C# library built at [CAKE] to facilitate the process of managing provisioned throughput in DynamoDB. It is designed to automatically scale throughput up or down according to a simple set of rules. We found it to be extremely useful in our environment, and have decided to share it with the general community.

[Sample Library Usage]

## Version
1.0.0

## Tech
DynamoDBAutoScale relies on the following:
 - .NET Framework 4
 - AWSSDK 2.3.18.0

## Usage Examples
An example use case is outlined below:
```
<?xml version="1.0" encoding="UTF-8"?>
<Rules xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" type="DynamoDBAutoScale.ConfigMapping.Rules, DynamoDBAutoScale">
	<Rule>
		<LookBackMinutes>15</LookBackMinutes>
		<DecreaseFrequency>Immediate</DecreaseFrequency>
		<Includes>
			<Tables>
				<Table>traffic_*</Table>
			</Tables>
			<Indexes>
				<Index>*</Index>
			</Indexes>
		</Includes>
		<Excludes>
			<Tables>
				<Table>*_14*</Table>
			</Tables>
		</Excludes>
		<Read>
			<MaximumThroughput>100</MaximumThroughput>
			<MinimumThroughput>10</MinimumThroughput>
			<DecreaseCombinationModifier>10</DecreaseCombinationModifier>
			<Increase>
				<Threshold>10</Threshold>
				<Amount>20</Amount>
			</Increase>
			<Decrease>
				<Threshold>50%</Threshold>
				<Amount>30%</Amount>
				<BlockoutTimeFrames>
					<BlockoutTimeFrame>
						<Start>1:00</Start>
						<End>2:00</End>
					</BlockoutTimeFrame>
					<BlockoutTimeFrame>
						<Start>3:00</Start>
						<End>4:00</End>
					</BlockoutTimeFrame>
				</BlockoutTimeFrames>
			</Decrease>
		</Read>
		<Write>
			<MaximumThroughput>500</MaximumThroughput>
			<MinimumThroughput>50</MinimumThroughput>
			<DecreaseCombinationModifier>10</DecreaseCombinationModifier>
			<Increase>
				<Threshold>10%</Threshold>
				<Amount>50</Amount>
			</Increase>
			<Decrease>
				<Threshold>30</Threshold>
				<Amount>25</Amount>
			</Decrease>
		</Write>
	</Rule>
</Rules>
```

##### LookBackMinutes (default: 10)
The formula used to calculate average consumed throughput, **Sum(Throughput) / Seconds**, relies on this parameter. A default of **10** will be used if a value is not provided.

#### DecreaseFrequency (default: EvenSpread)
3 options are available for this parameter:
 - EvenSpread: Evenly divide decrement allowances left throughout the rest of the day. Amazon allows for 4 decreases per UTC day, so at the start of everyday EvenSpread will allow 1 decrement every 4.8 hours.
 - Immediate: Decreases your throughput whenever a rule is matched.
 - Custom: Lets you specify a set time frame until another decrement is allowed. Currently supports minutes and hours (e.g. 30 minutes, 2 hours, etc.).

#### Includes/Excludes
Allows you to specify which tables/indexes a rule will match to via a simple wildcard (*****) pattern scheme. A general naming convension in DynamoDB is to suffix tables with a formatted date string. Through utilizing a wildcard pattern, you can easily match a rule to multiple tables created across multiple time frames (e.g. using a pattern of **traffic_15*** will match to both tables **traffic_1501** and **traffic_1502**).

#### Maximum/Minimum Throughput
Limits your throughput adjustment range so you don't incur unintended usage costs.

#### Threshold (default: 0)
The limit of units which the current consumed throughput must reach before a rule can be applied. This parameter allows for both a static unit count (**<Threshold>10</Threshold>**) or a percentage of current capacity (**<Threshold>20%</Threshold>**).

This setting is relative to the current provisioned capacity, therefore given a provisioned capacity of 100:
 - A rule with an increase threshold of **<Threshold>10</Threshold>** will not be activated unless the current consumed capacity is at 90 or above (100 - 10).
 - A rule with a decrease threshold of **<Threshold>20%</Threshold>** will not be activated unless the current consumed capcity is below 80 (100 - 20).

#### Amount (default: 0)
The amount of units to increase/decrease provisioned throughput by when a threshold is reached. This parameter allows for both a static unit count (**<Amount>30</Amount>**) or a percentage of current capacity (**<Amount>40%</Amount>**).

This settnig is relative to the current provisioned capacity, therefore, given a provisioned capacity of 200:
 - An increase amount of **<Amount>30</Amount>** will update the capacity to 230.
 - A decrease amount of **<Amount>40%</Amount>** will update the capacity to 120.

#### BlockoutTimeFrames
You can prevent the service from running during certain periods of the day by explicitly calling out this parameter. For instance, if you have an export job scheduled at 2 AM and are okay with it exhausting your alloted read throughput capacity, the following configuration will stop the rule from being activated between 2-3 AM.
```
<BlockoutTimeFrame>
	<Start>2:00</Start>
	<End>3:00</End>
</BlockoutTimeFrame>
```

#### DecreaseCombinationModifier (default: 0)
This is a **soft** threshold that will be used whenever one of the throughputs is eligible for a decrement adjustment. Due to Amazon's limited allowance of 4 decreases per UTC day, this setting was created to utilize them as efficiently as possible.

Given the following rule setup:
 - Provisioned throughput of 100 reads and writes.
 - Read decrease threshold of 10.
 - Write decrease threshold of 10

A read consumption of 80 will trigger the rule, and a write consumption of 93 will not. However, it would be activated if a decrease combination modifier of 5 was added to the write rule, because it is now within the specified range (93 < (100 - (10 - 5))).

## Contributors
 - [Dave Stewart]
 - [Rick Lee]

## License
Copyright 2015 CAKE
 
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
 
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.

[CAKE]: http://getcake.com/
[Sample Library Usage]: https://github.com/cake-labs
[Dave Stewart]: https://www.linkedin.com/in/daveastewart
[Rick Lee]: https://www.linkedin.com/in/rickkuanlee